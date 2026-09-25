use std::{ffi::OsStr, fs::File, io::Read, path::{Path, PathBuf}};

use zip::ZipArchive;

enum ArchiveInner {
    Zip(ZipArchive<File>),
    SevenZ {
        archive: sevenz_rust2::ArchiveReader<File>,
        path: PathBuf,
    },
    Rar(PathBuf),
}

pub struct Archive(ArchiveInner);

impl Archive {
    pub fn open(path: &Path) -> anyhow::Result<Archive> {
        if !path.is_file() {
            return Err(anyhow::anyhow!("path is not a file"));
        }
        match path.extension().and_then(OsStr::to_str) {
            Some("zip") => {
                let file = File::open(path)?;
                let archive = zip::ZipArchive::new(file)?;
                Ok(Archive(ArchiveInner::Zip(archive)))
            },
            Some("7z") => {
                let reader = sevenz_rust2::ArchiveReader::open(path, sevenz_rust2::Password::empty())?;
                Ok(Archive(ArchiveInner::SevenZ {
                    archive: reader,
                    path: path.to_path_buf()
                }))
            },
            Some("rar") => {
                Ok(Archive(ArchiveInner::Rar(path.to_path_buf())))
            },
            _ => Err(anyhow::anyhow!("unsupported archive format")),
        }
    }

    pub fn has_name(&mut self, name: &str) -> anyhow::Result<bool> {
        match &mut self.0 {
            ArchiveInner::Zip(archive) => {
                for i in 0..archive.len() {
                    let file = archive.by_index(i)?;
                    if Path::new(file.name())
                        .file_name()
                        .and_then(OsStr::to_str) == Some(name)
                    {
                        return Ok(true);
                    }
                }
                Ok(false)
            },
            ArchiveInner::SevenZ { archive, .. } => {
                Ok(
                    archive.archive()
                        .files
                        .iter()
                        .any(|f| Path::new(f.name()).file_name().and_then(OsStr::to_str) == Some(name))
                )
            },
            ArchiveInner::Rar(path) => {
                for file in unrar::Archive::new(path).open_for_listing()? {
                    let file = file?;
                    if file.filename
                        .file_name()
                        .and_then(OsStr::to_str) == Some(name)
                    {
                        return Ok(true);
                    }
                }
                Ok(false)
            },
        }
    }

    pub fn has_path(&mut self, path: impl AsRef<Path>) -> anyhow::Result<bool> {
        match &mut self.0 {
            ArchiveInner::Zip(archive) => {
                match archive.by_path(path) {
                    Ok(_) => Ok(true),
                    Err(e) => match e {
                        zip::result::ZipError::FileNotFound => Ok(false),
                        e => Err(e.into())
                    }
                }
            },
            ArchiveInner::SevenZ { archive, .. } => {
                Ok(
                    archive.archive()
                        .files
                        .iter()
                        .any(|f| Path::new(f.name()) == path.as_ref())
                )
            },
            ArchiveInner::Rar(archive) => {
                for file in unrar::Archive::new(archive).open_for_listing()? {
                    let file = file?;
                    if file.filename == path.as_ref() {
                        return Ok(true);
                    }
                }
                Ok(false)
            },
        }
    }

    pub fn read_path(&mut self, path: impl AsRef<Path>) -> anyhow::Result<Vec<u8>> {
        match &mut self.0 {
            ArchiveInner::Zip(archive) => {
                let mut file = archive.by_path(path)?;
                let mut data = Vec::new();
                file.read_to_end(&mut data)?;
                return Ok(data);
            },
            ArchiveInner::SevenZ { archive, .. } => {
                let file = path.as_ref()
                    .to_str()
                    .ok_or(anyhow::anyhow!("path contains non-UTF-8 characters"))?;
                archive.read_file(file).map_err(anyhow::Error::from)
            },
            ArchiveInner::Rar(archive) => {
                let mut archive = unrar::Archive::new(archive).open_for_processing()?;
                loop {
                    match archive.read_header()? {
                        Some(header) => {
                            if header.entry().filename == path.as_ref()  {
                                let (data, _) = header.read()?;
                                return Ok(data);
                            } else {
                                archive = header.skip()?;
                            }
                        }
                        None => break,
                    }
                }
                Err(anyhow::anyhow!("file not found in archive"))
            },
        }
    }

    /// Reject the whole archive if any entry looks unsafe to extract: an
    /// absolute path, a Windows drive-letter or UNC prefix, or a `..`
    /// component (checked against a backslash-normalized copy of the raw
    /// entry name, so a Windows-made archive's `..\..\evil.txt` is caught
    /// the same as `../../evil.txt`). Also rejects symlink entries outright
    /// -- mods never need them, and this crate can only detect them
    /// cleanly for zip.
    ///
    /// Not every backend sanitizes paths itself (zip does; sevenz_rust2 and
    /// unrar don't), so this is the actual safety net, not just a nicety.
    pub fn validate_entries(&mut self) -> anyhow::Result<()> {
        for entry in self.iter()? {
            let entry = entry?;

            if entry.is_symlink() {
                return Err(anyhow::anyhow!(
                    "archive contains unsafe path: symlink entry \"{}\"",
                    entry.path().display()
                ));
            }

            validate_entry_path(entry.path())?;
        }
        Ok(())
    }

    pub fn extract_to(&mut self, path: impl AsRef<Path>) -> anyhow::Result<()> {
        let path = path.as_ref();

        if !path.is_dir() {
            return Err(anyhow::anyhow!("path is not a directory"));
        }

        self.validate_entries()?;

        let result = match &mut self.0 {
            ArchiveInner::Zip(archive) => {
                archive.extract(path).map_err(anyhow::Error::from)
            },
            ArchiveInner::SevenZ { path: archive, .. } => {
                // sevenz_rust2 does not support extraction from an open ArchiveReader,
                // so we re-open the file here.
                sevenz_rust2::decompress_file(archive, path).map_err(anyhow::Error::from)
            },
            ArchiveInner::Rar(archive) => {
                let mut archive = unrar::Archive::new(archive).open_for_processing()?;
                loop {
                    match archive.read_header()? {
                        Some(header) => archive = header.extract_with_base(path)?,
                        None => break,
                    }
                }
                Ok(())
            },
        };

        result?;

        // Belt and braces: re-check what actually landed on disk, since
        // sevenz_rust2/unrar don't sanitize on their own and we can't
        // detect a symlink entry ahead of time for every format.
        if let Err(e) = verify_extraction_contained(path) {
            let _ = std::fs::remove_dir_all(path);
            return Err(e);
        }

        Ok(())
    }

    pub fn iter<'a>(&'a mut self) -> anyhow::Result<ArchiveIter<'a>> {
        ArchiveIter::new(&mut self.0)
    }
}

fn validate_entry_path(path: &Path) -> anyhow::Result<()> {
    let original = path.to_string_lossy();
    let normalized = original.replace('\\', "/");

    if normalized.starts_with('/') {
        return Err(anyhow::anyhow!("archive contains unsafe path: {}", original));
    }

    let bytes = normalized.as_bytes();
    if bytes.len() >= 2 && bytes[0].is_ascii_alphabetic() && bytes[1] == b':' {
        return Err(anyhow::anyhow!("archive contains unsafe path: {}", original));
    }

    if normalized.split('/').any(|component| component == "..") {
        return Err(anyhow::anyhow!("archive contains unsafe path: {}", original));
    }

    Ok(())
}

fn verify_extraction_contained(dir: &Path) -> anyhow::Result<()> {
    let canonical_base = std::fs::canonicalize(dir)?;
    verify_dir_contained(dir, &canonical_base)
}

fn verify_dir_contained(dir: &Path, canonical_base: &Path) -> anyhow::Result<()> {
    for entry in std::fs::read_dir(dir)? {
        let entry = entry?;
        let file_type = entry.file_type()?;
        let entry_path = entry.path();

        if file_type.is_symlink() {
            return Err(anyhow::anyhow!("archive contains unsafe path: symlink at {:?}", entry_path));
        } else if file_type.is_dir() {
            verify_dir_contained(&entry_path, canonical_base)?;
        } else if file_type.is_file() {
            let canonical = std::fs::canonicalize(&entry_path)?;
            if !canonical.starts_with(canonical_base) {
                return Err(anyhow::anyhow!("archive contains unsafe path: {:?} escapes the destination", entry_path));
            }
        }
    }
    Ok(())
}

enum IterInner<'a> {
    Zip { 
        archive: &'a mut ZipArchive<File>,
        index: usize,
    },
    SevenZ { 
        archive: &'a sevenz_rust2::Archive,
        index: usize,
    },
    Rar {
        archive: unrar::OpenArchive<unrar::List, unrar::CursorBeforeHeader>,
        done: bool,
    },
}

pub struct ArchiveIter<'a>(IterInner<'a>);

impl<'a> ArchiveIter<'a> {
    fn new(archive: &'a mut ArchiveInner) -> anyhow::Result<ArchiveIter<'a>> {
        Ok(ArchiveIter(match archive {
            ArchiveInner::Zip(archive) => IterInner::Zip {
                archive,
                index: 0
            },
            ArchiveInner::SevenZ { archive, .. } => IterInner::SevenZ {
                archive: archive.archive(),
                index: 0
            },
            ArchiveInner::Rar(archive) => {
                let archive = unrar::Archive::new(archive).open_for_listing()?;
                IterInner::Rar {
                    archive,
                    done: false
                }
            },
        }))
    }
}

impl<'a> Iterator for ArchiveIter<'a> {
    type Item = anyhow::Result<ArchiveEntry>;

    fn next(&mut self) -> Option<Self::Item> {
        match &mut self.0 {
            IterInner::Zip { archive, index } => {
                if *index < archive.len() {
                    let entry = archive.by_index(*index)
                        .map(|file| ArchiveEntry {
                            is_directory: file.is_dir(),
                            // Raw, unsanitized name -- mangled_name() would
                            // silently strip `..` and hide a traversal
                            // attempt instead of letting validate_entry_path
                            // reject it.
                            path: PathBuf::from(file.name()),
                            is_symlink: file.is_symlink(),
                        })
                        .map_err(anyhow::Error::from);
                    *index += 1;
                    Some(entry)
                } else {
                    None
                }
            },
            IterInner::SevenZ { archive, index } => {
                if *index < archive.files.len() {
                    let file = &archive.files[*index];
                    *index += 1;
                    Some(Ok(ArchiveEntry {
                        is_directory: file.is_directory(),
                        path: PathBuf::from(file.name()),
                        // sevenz_rust2 doesn't expose a clean symlink flag;
                        // caught by verify_extraction_contained instead.
                        is_symlink: false,
                    }))
                } else {
                    None
                }
            },
            IterInner::Rar { archive, done } => {
                if *done {
                    return None;
                }
                match archive.next() {
                    None => None,
                    Some(Err(e)) => {
                        *done = true;
                        Some(Err(anyhow::Error::from(e)))
                    }
                    Some(Ok(entry)) => Some(Ok(ArchiveEntry {
                        is_directory: entry.is_directory(),
                        path: entry.filename,
                        // Same story as 7z: unrar doesn't expose this cleanly.
                        is_symlink: false,
                    }))
                }
            },
        }
    }
}

impl<'a> std::iter::FusedIterator for ArchiveIter<'a> {}

pub struct ArchiveEntry {
    is_directory: bool,
    path: PathBuf,
    is_symlink: bool,
}

impl ArchiveEntry {
    pub fn is_directory(&self) -> bool {
        self.is_directory
    }

    pub fn path(&self) -> &Path {
        &self.path
    }

    /// Only zip entries can be identified reliably here (unix mode bits);
    /// 7z/rar entries always report `false` even if they are one.
    pub fn is_symlink(&self) -> bool {
        self.is_symlink
    }
}
#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    fn make_zip(dir: &Path, file_name: &str, entries: &[(&str, &[u8])]) -> PathBuf {
        let path = dir.join(file_name);
        let file = File::create(&path).unwrap();
        let mut writer = zip::ZipWriter::new(file);
        let options = zip::write::SimpleFileOptions::default();
        for (name, data) in entries {
            writer.start_file(*name, options).unwrap();
            writer.write_all(data).unwrap();
        }
        writer.finish().unwrap();
        path
    }

    fn make_7z(dir: &Path, file_name: &str, entries: &[(&str, &[u8])]) -> PathBuf {
        let path = dir.join(file_name);
        let mut writer = sevenz_rust2::ArchiveWriter::create(&path).unwrap();
        for (name, data) in entries {
            let entry = sevenz_rust2::ArchiveEntry::new_file(name);
            writer
                .push_archive_entry(entry, Some(std::io::Cursor::new(*data)))
                .unwrap();
        }
        writer.finish().unwrap();
        path
    }

    fn fresh_dest(tmp: &Path, name: &str) -> PathBuf {
        let dest = tmp.join(name);
        std::fs::create_dir(&dest).unwrap();
        dest
    }

    #[test]
    fn rejects_parent_dir_traversal_in_zip() {
        let tmp = tempfile::tempdir().unwrap();
        let zip_path = make_zip(tmp.path(), "mal.zip", &[("../evil.txt", b"evil")]);
        let dest = fresh_dest(tmp.path(), "dest");

        let mut archive = Archive::open(&zip_path).unwrap();
        let err = archive.extract_to(&dest).unwrap_err();
        assert!(err.to_string().contains("unsafe path"), "{err}");

        assert!(!tmp.path().join("evil.txt").exists());
        assert!(!dest.join("evil.txt").exists());
    }

    #[test]
    fn rejects_parent_dir_traversal_in_7z() {
        let tmp = tempfile::tempdir().unwrap();
        let sz_path = make_7z(tmp.path(), "mal.7z", &[("../evil.txt", b"evil")]);
        let dest = fresh_dest(tmp.path(), "dest");

        let mut archive = Archive::open(&sz_path).unwrap();
        let err = archive.extract_to(&dest).unwrap_err();
        assert!(err.to_string().contains("unsafe path"), "{err}");

        assert!(!tmp.path().join("evil.txt").exists());
        assert!(!dest.join("evil.txt").exists());
    }

    #[test]
    fn extracts_normal_archive_fine() {
        let tmp = tempfile::tempdir().unwrap();
        let zip_path = make_zip(
            tmp.path(),
            "good.zip",
            &[("dir/file.txt", b"hello"), ("root.txt", b"!")],
        );
        let dest = fresh_dest(tmp.path(), "dest");

        let mut archive = Archive::open(&zip_path).unwrap();
        archive.extract_to(&dest).unwrap();

        assert_eq!(
            std::fs::read_to_string(dest.join("dir/file.txt")).unwrap(),
            "hello"
        );
        assert_eq!(std::fs::read_to_string(dest.join("root.txt")).unwrap(), "!");
    }
}

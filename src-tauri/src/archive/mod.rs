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
                match archive.by_name(name) {
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
                        .find(
                            |f| Path::new(f.name())
                                .file_name()
                                .map(|n| n.to_str())
                                .flatten() == Some(name)
                        )
                        .is_some()
                )
            },
            ArchiveInner::Rar(path) => {
                for file in unrar::Archive::new(path).open_for_listing()? {
                    let file = file?;
                    if file.filename
                        .file_name()
                        .map(|n| n.to_str())
                        .flatten() == Some(name)
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
                        .find(|f| Path::new(f.name()) == path.as_ref())
                        .is_some()
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
                let file = &path.as_ref().to_string_lossy();
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

    pub fn extract_to(&mut self, path: impl AsRef<Path>) -> anyhow::Result<()> {
        if !path.as_ref().is_dir() {
            return Err(anyhow::anyhow!("path is not a directory"));
        }
        match &mut self.0 {
            ArchiveInner::Zip(archive) => {
                archive.extract(path.as_ref()).map_err(anyhow::Error::from)
            },
            ArchiveInner::SevenZ { path: archive, .. } => {
                sevenz_rust2::decompress_file(archive, path).map_err(anyhow::Error::from)
            },
            ArchiveInner::Rar(archive) => {
                let mut archive = unrar::Archive::new(archive).open_for_processing()?;
                loop {
                    match archive.read_header()? {
                        Some(header) => archive = header.extract_with_base(path.as_ref())?,
                        None => break,
                    }
                }
                Ok(())
            },
        }
    }

    pub fn iter<'a>(&'a mut self) -> anyhow::Result<ArchiveIter<'a>> {
        ArchiveIter::new(&mut self.0)
    }
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
    Rar(unrar::OpenArchive<unrar::List, unrar::CursorBeforeHeader>),
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
                IterInner::Rar(archive)
            },
        }))
    }
}

impl<'a> Iterator for ArchiveIter<'a> {
    type Item = ArchiveEntry;

    fn next(&mut self) -> Option<Self::Item> {
        match &mut self.0 {
            IterInner::Zip { archive, index } => {
                if *index < archive.len() {
                    let file = archive.by_index(*index).ok()?;
                    *index += 1;
                    Some(ArchiveEntry {
                        is_directory: file.is_dir(),
                        path: file.mangled_name()
                    })
                } else {
                    None
                }
            },
            IterInner::SevenZ { archive, index } => {
                if *index < archive.files.len() {
                    let file = &archive.files[*index];
                    *index += 1;
                    Some(ArchiveEntry {
                        is_directory: file.is_directory(),
                        path: PathBuf::from(file.name())
                    })
                } else {
                    None
                }
            },
            IterInner::Rar(archive) => {
                archive.next()
                    .map(|result| result.ok())
                    .flatten()
                    .map(|entry| ArchiveEntry {
                        is_directory: entry.is_directory(),
                        path: entry.filename
                    })
            },
        }
    }
}

pub struct ArchiveEntry {
    is_directory: bool,
    path: PathBuf,
}

impl ArchiveEntry {
    pub fn is_directory(&self) -> bool {
        self.is_directory
    }

    pub fn path(&self) -> &Path {
        &self.path
    }
}
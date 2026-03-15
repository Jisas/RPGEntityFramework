# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-03-14
### Added
- **RPG Database Editor**: A comprehensive management window built with UI Toolkit for all RPG definitions.
- **Search & Filter System**: Real-time filtering in the sidebar for quick access to specific definitions.
- **Automated Migration Tool**: New utility to move existing assets from local `Assets/` to the package's internal `Resources/` structure.
- **Test Builder & Validator**: Integrated tool to scan the database for missing references (broken links), missing icons, and data integrity issues.
- **Dynamic Asset Creation**: Automated creation and registration of ScriptableObjects within category-specific folders.

### Fixed
- **UI Refresh Sync**: Resolved an issue where newly created assets wouldn't appear in the `ListView` until a manual refresh.
- **AssetDatabase Migration Error**: Fixed the "Parent directory is not in asset database" warning by ensuring directory structures are registered before batch editing operations.
- **Path Resolution**: Improved `_basePath` handling to support both local development and embedded package paths.
- **UI Toolkit Integration**: Fixed incorrect references and ensured full compatibility with Unity 6's UI Toolkit features.

### Changed
- Improved the `MoveCategory` logic to handle mass migration inside `StartAssetEditing` blocks for better performance.
- Refactored `CreateNewEntity` to automatically update local UI caches and scroll to the newly created item.

## [0.1.0] - 2025-06-22
### Added
- Initial package structure.
- Basic Race ScriptableObject definitions.
- Early implementation of the Race Editor window.
- Basic documentation and example assets.
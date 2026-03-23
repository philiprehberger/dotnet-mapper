# Changelog

## 0.1.4 (2026-03-22)

- Fix changelog formatting

## 0.1.3 (2026-03-17)

- Rename Install section to Installation in README per package guide

## 0.1.2 (2026-03-16)

- Add badges, Development section to README
- Add GenerateDocumentationFile, RepositoryType, PackageReadmeFile to .csproj

## 0.1.0 (2026-03-15)

- Initial release
- Convention-based property mapping by name
- Fluent configuration API with `Map`, `Ignore`, and `MapFrom` overrides
- Automatic flattening support (e.g., `Address.City` -> `AddressCity`)
- `MapToAttribute` marker for documentation and discovery
- Runtime diagnostics with `GetUnmappedProperties` and `ValidateMapping`

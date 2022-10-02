# Unity Sass Importer

Custom `ScriptedImporter` [Sass](https://sass-lang.com/). Enables `.scss` and `.sass` files to be used within Unity projects in conjuction with UI Tookit style sheets. It doesn't create any new files, but rather imports file as StyleSheet.

Since UI Toolkit is built on web technologies, we can leverage already existing stack to speed up development and iteration process.

- Should support everything `sass` language offers
- Currently supports only `@import` statement with files encapsulated with `'` (single quote)
- **Will break with circular imports**
- Only files **not** starting with `_` are imported as style sheets
- Works only one way, `.scss` files need to be written manually, anything changed from UI Builder will be overwritten
- Currently relies on C# reflection to instantiate `StyleSheetImporterImpl`, which means it can break with any update of Unity / UI Toolkit

## Requirements
- Preinstalled `sass` command available from command line

## Platforms
- Tested on Windows running Unity 2021.3.2f1 and 2022.1.10f

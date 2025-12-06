; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

Rule ID | Category         | Severity | Notes
--------|------------------|----------|-----------------------------
OBS001  | ObservableValue  | Error    | ObservableValue can only be used on private fields
OBS002  | ObservableValue  | Error    | ObservableValue fields must start with '_' or a lower-case letter
OBS003  | ObservableValue  | Error    | ObservableValue cannot overwrite existing members
OBS004  | ObservableValue  | Error    | ObservableValue can only be used on partial classes
OBS005  | ObservableValue  | Error    | ObservableValue field declarations must declare exactly one variable


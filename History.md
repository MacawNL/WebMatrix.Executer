## 1.4.0.0 (2012-10-13)

* Solved issue with InitializeTabs()

## 1.3.0.0 (2012-10-12)

* Added WriteNoParse() and WriteLineNoParse() functions for output that is not parsed for warnings and errors.

## 1.2.0.0 (2012-10-12)

* Fixed deployment issue, wrong assemblies were included.

## 1.1.0.0 (2012-10-11)

* Added support for multiple scenario's using new Start() and End() methods in interface: 
  * execute single command
  * execute multiple commands and writelines
  * do actions yourself, output with writelines that are parsed
* Added UIThreadDispatcher property to interface for executing actions on the UI thread
* See [solved issues](https://github.com/MacawNL/WebMatrix.Executer/issues?labels=&milestone=1&state=closed) on GitHub.

## 1.0.0.0 (2012-9-28)

* First release

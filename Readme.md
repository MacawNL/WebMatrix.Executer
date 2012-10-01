NuGet package: [http://nuget.org/packages/WebMatrix.Executer](http://nuget.org/packages/WebMatrix.Executer)

DocumentUp version of the documentation: http://macawnl.github.com/WebMatrix.Executer/

# WebMatrix.Executer

## Introduction

The WebMatrix Extensibility Model does not support out of the box the executing
of applications and scripts, an output pane, and an Error List pane that may 
be used by WebMatrix Extension Writers. Although there is an Error List pane 
available, this pane may not be used by custom extensions.

**WebMatrix.Executer** provides a library that can be included in your own baked
WebMatrix extension to execute applications and PowerShell scripts, send 
the output in realtime to a WebMatrix **Output** pane, and parse the output for 
errors and warnings that are displayed in a WebMatrix **Errors & Warnings** pane
with just a few lines of code. Multiple extensions using **WebMatrix.Executer**
will use the same panes.

## Functionality
**WebMatrix.Executer** provides two panes:

- The Output pane where the output of an executing program is captured (StdOut, StdErr)
- The Errors & Warnings pane where the errors and warnings found by parsing the output are reported

**WebMatrix.Executer** can execute applications and PowerShell scripts asynchronuously, and capture 
the output and the errors & warnings in realtime to the two panes.
Applications and scripts are executed under a "task source", this is a simple string identifier that specifies
to which "group" an application or script belongs. The **Output** pane can be switched to the output of a task 
source, the **Errors & Warnings** pane can be filtered on the task source.

### Output pane
![The Output pane](https://raw.github.com/MacawNL/WebMatrix.Executer/master/DocumentationImages/OutputPane.png)
[Enlarge image](https://raw.github.com/MacawNL/WebMatrix.Executer/master/DocumentationImages/OutputPane.png)

- The Output pane support multiple task sources
- When an executing task has output, the Output pane is made visible
- Output of an executing task (StdOut, StdErr) is displayed real-time
- The output per task source can be selected through a drop down box
- The output of a task source can be cleared
- The output of a task source can be word-wrapped
- A task (running program or script) executing under a task source can be canceled
- When the output of a task contains errors or warnings, **WebMatrix.Executer** switches
   automatically to the Errors & Warnings pane

### Errors & Warnings pane
![The Errors & Warnings pane](https://raw.github.com/MacawNL/WebMatrix.Executer/master/DocumentationImages/ErrorsAndWarningsPane.png)
[Enlarge image](https://raw.github.com/MacawNL/WebMatrix.Executer/master/DocumentationImages/ErrorsAndWarningsPane.png)

- The Errors & Warnings pane displays the error and warning messages of all task sources
- Output of an executing task (StdOut, StdErr) is parsed real-time for erros and warnings
- Error and warning messages are visualized using an error and warning icon
- The messages can be filtered on task source (all, one), errors, warnings
- The messages can be cleared
- If a message contains a reference to a file, the path is displayed relative to the root of the WebMatrix (~/...)
- If a message contains a reference to a file, and the file can't be resolved, the original file path is displayed
- If a message is clicked, the corresponding file is opened in the editor at the correct line (if known) 
   and correct column (if known)
- If a message is clicked and the file can't be resolved, a message box is displayed specifying the file, line and column
- If a message contains an url, the url is used as a help link and a help icon appears

## Background Information

[WebMatrix extensions](http://extensions.webmatrix.com/) are [MEF](http://msdn.microsoft.com/en-us/library/dd460648.aspx)
extensions that are loaded dynamically from the folder `%USERPROFILE%\AppData\Local\Microsoft\WebMatrix\Extensions\20`.
WebMatrix extensions are easy to develop. The [How to Create Extensions](http://extensions.webmatrix.com/documentation_2)
page by Microsoft gets you started with pointers to a Visual Studio template and the WebMatrix Extensibility API documentation.

**WebMatrix.Executer** provides a good base when writing extensions that need a kind of "compilation" 
with output and error reporting. Because the **WebMatrix.Executer** functionality could be used by 
multiple extensions, also extensions not written by you, and we don't want multiple output tabs, only 
one version of the code should be running. To accomplish this you will program against an interface 
with available methods that is independent from the actual implementation, and **Webmatrix.Executer**
will make sure that only a single instance of the implementation assembly is running, and that always
the newest version available in all extensions will be running.

The interface is defined in the `DesignFactory.WebMatrix.IExecuter.dll` assembly . The `DesignFactory.WebMatrix.ExecuterFactory.dll` 
assembly contains a smart piece of code that makes sure that the first WebMatrix extension that uses **WebMatrix.Executer**,
and that is loaded through MEF, loads the latest version of the **WebMatrix.Executer**
implementation assembly `DesignFactory.WebMatrix.Executer.dll` as available in each of the extension folders. The interface will never change 
or remove methods signatures, it will only be extended to ensure backwards 
compatibility to older extensions while enabling innovations in 
WebMatrix.Executer itself.

## Distribution: NuGet
**WebMatrix.Executer** is distributed as a [NuGet](http://nuget.org/packages/WebMatrix.Executer) 
package. The package is named **WebMatrix.Executer**. The **WebMatrix.Executer** NuGet 
package consists of the following assemblies:

- `DesignFactory.WebMatrix.IExecuter.dll`

This is the interface assembly to program against. This assembly will be 
referenced by your project.

- `DesignFactory.WebMatrix.ExecuterFactory.dll`

This assembly contains the `ExecuterFactory()` function to load the newest version of 
the `DesignFactory.WebMatrix.Executer.dll` implementation assembly. This 
assembly will be referenced by your project.

- `DesignFactory.WebMatrix.Executer.dll`

This assembly contains the actual implementation of the interface, and the interface 
itself as an embedded type. See for more background information:
http://msdn.microsoft.com/en-us/library/dd409610.aspx.
This assembly is NOT referenced, but will be copied to the output directory
alongside your extension assembly, the interface assembly and the factory assembly.

## Usage

To enable **WebMatrix.Executer** functionality, in your extension,
the extensions needs an implementation as simple as:

```cs
namespace MyLittleWebMatrixExtension
{
    /// <summary>
    /// A sample WebMatrix extension.
    /// </summary>
    [Export(typeof(Extension))]
    public class MyLittleWebMatrixExtension : Extension
    {
        /// <summary>
        /// Stores a reference to the WebMatrix host interface.
        /// </summary>
        private IWebMatrixHost _webMatrixHost;

        /// <summary>
        /// Reference to the EditorTaskPanelService.
        /// </summary>
        private IEditorTaskPanelService _editorTaskPanel;

        [Import(typeof(IEditorTaskPanelService))]
        private IEditorTaskPanelService EditorTaskPanelService
        {
            get
            {
                return _editorTaskPanel;
            }
            set
            {
                _editorTaskPanel = value;
            }
        }

        DesignFactory.WebMatrix.IExecuter.IExecuter _executer;

        /// <summary>
        /// Initializes new instance of MyLittleWebMatrixExtension.
        /// </summary>
        public MyLittleWebMatrixExtension()
            : base("MyLittleWebMatrixExtension")
        {
        }

        /// <summary>
        /// Called to initialize the extension.
        /// </summary>
        /// <param name="host">WebMatrix host interface.</param>
        /// <param name="initData">Extension initialization data.</param>
        protected override void Initialize(IWebMatrixHost host, 
                                           ExtensionInitData initData)
        {
            _webMatrixHost = host;

            // Add a simple button to the Ribbon
            initData.RibbonItems.Add(
                new RibbonGroup("DoIt Group",new RibbonItem[]
                   {new RibbonButton("Just DoIt",
                    new DelegateCommand(HandleRibbonButtonInvoke),
                    null, _starImageSmall, _starImageLarge)}));
        
            _executer = DesignFactory.WebMatrix.ExecuterFactory.GetExecuter(
                            "DoIt", _webMatrixHost, _editorTaskPanel);
        }

        /// <summary>
        /// Called when the Ribbon button is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        private async void HandleRibbonButtonInvoke(object parameter)
        {
            string scriptDoIt = Path.Combine(_webMatrixHost.WebSite.Path, 
            	@"WebMatrixTests\DoIt.bat");
            await _executer.RunAsync("cmd.exe", "/c \"" + scriptDoIt + "\"");
        }	    
    }
}
```

Note the `EditorTaskPanelService` stuff, we need this to be able to create the
Output and Errors & Warnings panes.

The `DesignFactory.WebMatrix.ExecuterFactory.GetExecuter(...)` call requires three
parameters:

* `string tasksource`:  Specifies the group name where tasks from this executer
are executed under. This name is shown as selector in the Output pane and
can be used to filter errors and warnings.

* `IWebMatrixHost webMatrixHost`: the WebMatrix host, available as parameter to
the `Initialize()` method in your WebMatrix extension.

* `IEditorTaskPanelService editorTaskPanelService`: Reference to the 
`EditorTaskPanelService`, see example code above on how to get it.

In the `GetExecuter(...)` call the **WebMatrix.Executer** system is initialized and the tabs are
created for the **Output** pane and **Errors & Warnings** pane.

The call to `GetExecuter(...)` returns an
instance of an object that implements the following interface:

```cs
namespace DesignFactory.WebMatrix.IExecuter
{
    public interface IExecuter
    {
        System.Threading.Tasks.Task<bool> RunAsync(string fileName, 
                                                   string arguments);
        System.Threading.Tasks.Task<bool> RunPowerShellAsync(string arguments);
        bool IsRunning();
        void Cancel();
        void Write(string format, params object[] args);
        void WriteLine(string format, params object[] args);

        // Only used by DesignFactory.WebMatrix.ExecuterFactory.GetExecuter()
        void Initialize(string tasksource, IWebMatrixHost webMatrixHost, 
                        IEditorTaskPanelService editorTaskPanelService);
        void InitializeTabs();
    }
}
```

These methods do the following:

`RunAsync`: Execute an application with parameters async. Returns true if
successful, false otherwise. Output (StdOut, Stderr) is written to output pane
and parsed for errors and warnings.
Example: 

```cs
string scriptDoIt = Path.Combine(_webMatrixHost.WebSite.Path, 
                                 @"WebMatrixTests\DoIt.bat");
var ok = await _executer.RunAsync("cmd.exe", "/c \"" + scriptDoIt + "\"");
```

`RunPowerShellAsync`: Execute a powershell command (in the arguments) in a
64 bits PowerShell process on a 64 bits Windows, and in a 32 bits PowerShell
process on a 32 bits Windows. Even if WebMatrix is running as 32 bits app.
Arguments are all the arguments as you would pass to PowerShell.exe.
Output (StdOut, Stderr) is written to output pane
and parsed for errors and warnings.

`IsRunning`: Returns true if executer is executing, false otherwise.

`Cancel`: Cancel the currently running process in the executer.

`Write`, `WriteLine`: Write output to the Output pane.

`Initialize`: Initialize the executer system. Called by the 
`ExecuterFactory.GetExecuter()` method with the parameters
given to `GetExecuter()`. Not for use by the extension developer.

`InitializeTabs`: Initialize the tabs for the executer system. Called by the
`ExecuterFactory.GetExecuter()` method. Not for use by the extension developer.

## Formatting errors & warnings

Any line containing the word 'error' or 'warning' is reported. We don't only parse output from Visual Studio/MSBuild,
but also from external tools that don't use the Visual Studio/MsBuild guidelines for reporting errors and warnings.
If the word 'error' or 'warning' is used in a filename, it should not be reported.

*If the word 'error' or 'warning' is used in a special construction, an ignore list with
regular expressions can be specified, for example to exclude the line 'On [0-9]+ lines an error or warning occured'.* 
(Not implemented yet, plumbing is available however)

If an error or warning complies to the Visual Studio/MSBuild error format, additional information is returned about the file,
line number where the error or warning occured.

Most important is that filenames containing the word error or warning are not reported as false positives.
The following characters are assumed to be common filename characters: a-zA-Z0-9_-.

Simple regular expression user for error/warning parsing:

```cs
static private readonly Regex simpleMessageExpression = new Regex
(
    String.Format(@"(?<CATEGORY>({0}error{0}|{0}warning{0}))", excludeCommonFilenameCharacters),
    RegexOptions.IgnoreCase
);
```

MSBuild compliant error/warning parsing:

```cs
static private readonly Regex originCategoryCodeTextExpression = new Regex
(
    // Beginning of line and any amount of whitespace.
    @"^\s*"
    // Match a [optional project number prefix 'ddd>'], single letter + colon + remaining filename, or
    // string with no colon followed by a colon.
    + @"(((?<ORIGIN>(((\d+>)?[a-zA-Z]?:[^:]*)|([^:]*))):)"
    // Origin may also be empty. In this case there's no trailing colon.
    + "|())"
    // Match the empty string or a string without a colon that ends with a space
    + "(?<SUBCATEGORY>(()|([^:]*? )))"
    // Match 'error' or 'warning' followed by a space.
    + @"(?<CATEGORY>(error|warning))\s*"
    // Match anything without a colon, followed by a colon
    + "(?<CODE>[^:]*):"
    // Whatever's left on this line, including colons.
    + "(?<TEXT>.*)$",
    RegexOptions.IgnoreCase
);
```

For more information on MSBuild compliant messages see [http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx](http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx).
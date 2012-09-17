Dear user,

Thanks for installing the WebMatrix.Executer package.

WebMatrix Extensibility Model does not support out of the box the executing 
of applications and scripts, an output pane, and an Error List pane that may 
be used by WebMatrix Extension Writers. Although there is an Error List pane 
available, this pane may not be used by extensions.

WebMatrix.Executer provides a library that can be included in your own baked
WebMatrix extension to execute applications and PowerShell scripts, send 
the output in realtime to a WebMatrix output pane, and parse the output for 
errors and warnings that are displayed in a WebMatrix Errors & warnings pane
with just a few lines of code. Multiple extensions using WebMatrix.Executer
will use the same panes.

Because this library can be used by multiple extensions, you program against 
an interface. A smart piece of provided code to be included into your extension
makes sure that the first extension that is loaded through MEF loads the latest
version of the WebMatrix.Executer library as available in any of the 
extensions. The interface will never change or remove methods signatures, it 
will only be extended to ensure backwards compatibility to older extensions 
while enabeling innovations in WebMatrix.Executer itself.

The WebMatrix.Executer NuGet package consists of:

- DesignFactory.WebMatrix.IExecuter.dll

This is the interface assembly to program against. The assembly will be 
referenced by your project.

- DesignFactory.WebMatrix.Executer.dll

This assembly contains the implementation of the interface, and the interface 
itself as an embedded type. See for more background information:
http://msdn.microsoft.com/en-us/library/dd409610.aspx.
This assembly is NOT referenced, but will be copied to the output directory
alongside your extension assembly and the interface assembly.

- DesignFactory.WebMatrix.ExecuterFactory.cs

This class file is added to your project and contains the code to load the
newest version of the DesignFactory.WebMatrix.Executer.dll implementation
assembly. To enable WebMatrix.Executer functionality, in your extension,
the extensions needs an implementation as simple as:

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
        /// Initializes a new instance of the MyLittleWebMatrixExtension class.
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
        protected override void Initialize(IWebMatrixHost host, ExtensionInitData initData)
        {
            _webMatrixHost = host;

            // Add your ribbon and context menu extensions
			// :
        
		    _executer = DesignFactory.WebMatrix.ExecuterFactory.GetExecuter("MyThingy", _webMatrixHost, _editorTaskPanel);
        }

		// :
	}
}

Happy Execution!

Serge van den Oever [Macaw]


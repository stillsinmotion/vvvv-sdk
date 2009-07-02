#region licence/info

//////project name
//vvvv plugin template

//////description
//basic vvvv node plugin template.
//Copy this an rename it, to write your own plugin node.

//////licence
//GNU Lesser General Public License (LGPL)
//english: http://www.gnu.org/licenses/lgpl.html
//german: http://www.gnu.de/lgpl-ger.html

//////language/ide
//C# sharpdevelop 

//////dependencies
//VVVV.PluginInterfaces.V1;
//VVVV.Utils.VColor;
//VVVV.Utils.VMath;

//////initial author
//vvvv group

#endregion licence/info


//use what you need
using System;
using System.Drawing;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Webinterface.Utilities;




//the vvvv node namespace
namespace VVVV.Nodes.HttpGUI.CSS
{




    
    /// <summary>
    /// css node font(css) class definition
    /// generates code to change css font values
    /// </summary>
    public class Font :BaseCssNode, IPlugin, IDisposable
    {





        #region field declaration


        // Track whether Dispose has been called.
        private bool FDisposed = false;

        //input pin declaration
        private IValueIn FFontSizeIn;
		private IColorIn FColorInput;
        private IStringIn FFontFamiliyIn;
        private IEnumIn FFontStyleIn;
        private IEnumIn FFontWeigthIn;
        private IValueIn FWordSpacingIn;
        private IValueIn FLetterSpacingIn;
        private IStringIn FFontStretchIn;
        private IEnumIn FTextDecortationIn;


        #endregion field declaration






        #region constructor/destructor



        /// <summary>
        /// the nodes constructor
        /// nothing to declare for this node
        /// </summary>
        public Font()
        {
            
        }



        /// <summary>
        /// Implementing IDisposable's Dispose method.
        /// Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }



       

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (FDisposed == false)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                }
                // Release unmanaged resources. If disposing is false,
                // only the following code is executed.

                FHost.Log(TLogType.Message, "Padding(CSS) Node is being deleted");

                // Note that this is not thread safe.
                // Another thread could start disposing the object
                // after the managed resources are disposed,
                // but before the disposed flag is set to true.
                // If thread safety is necessary, it must be
                // implemented by the client.
            }

            FDisposed = true;
        }

        
        /// <summary>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in WebTypes derived from this class.
        /// </summary>
        ~Font()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion constructor/destructor



 


        #region node name and infos


        private static IPluginInfo FPluginInfo;

        /// <summary>
        /// provide node infos
        /// </summary>
        public static IPluginInfo PluginInfo
        {
            get
            {
                if (FPluginInfo == null)
                {
                    // fill out nodes info
                    // see: http://www.vvvv.org/tiki-index.php?page=vvvv+naming+conventions
                    FPluginInfo = new PluginInfo();

                    // the nodes main name: use CamelCaps and no spaces
                    FPluginInfo.Name = "Font";
                    // the nodes category: try to use an existing one
                    FPluginInfo.Category = "HTTP";
                    // the nodes version: optional. leave blank if not
                    // needed to distinguish two nodes of the same name and category
                    FPluginInfo.Version = "CSS";

                    // the nodes author: your sign
                    FPluginInfo.Author = "phlegma";
                    // describe the nodes function
                    FPluginInfo.Help = "node for html page creation";
                    // specify a comma separated list of tags that describe the node
                    FPluginInfo.Tags = "";

                    // give credits to thirdparty code used
                    FPluginInfo.Credits = "";
                    // any known problems?
                    FPluginInfo.Bugs = "";
                    // any known usage of the node that may cause troubles?
                    FPluginInfo.Warnings = "";
					
                    // leave below as is
                    System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
                    System.Diagnostics.StackFrame sf = st.GetFrame(0);
                    System.Reflection.MethodBase method = sf.GetMethod();
                    FPluginInfo.Namespace = method.DeclaringType.Namespace;
                    FPluginInfo.Class = method.DeclaringType.Name;
                    // leave above as is
                }

                return FPluginInfo;
            }
        }


        #endregion node name and infos







        #region pin creation

        /// <summary>
        /// this method is called by vvvv when the node is created
        /// </summary>
        /// <param name="Host"></param>
        protected override void OnPluginHostSet()
        {
            // create inputs
            FHost.CreateValueInput("Font Size", 1, null, TSliceMode.Dynamic, TPinVisibility.True, out FFontSizeIn);
            FFontSizeIn.SetSubType(double.MinValue, double.MaxValue, 0.01, 1, false, false, false);

			FHost.CreateColorInput("Color", TSliceMode.Dynamic, TPinVisibility.True, out FColorInput);
            FColorInput.SetSubType(VColor.Black, false);

            FHost.CreateStringInput("FontFamiliy", TSliceMode.Dynamic, TPinVisibility.True, out FFontFamiliyIn);
            FFontFamiliyIn.SetSubType("Verdana", false);

            FHost.UpdateEnum("FontStyle", "normal ", new string[] { "normal ", "italic ", "oblique " });
            FHost.CreateEnumInput("FontStyle", TSliceMode.Single, TPinVisibility.True, out FFontStyleIn);
            FFontStyleIn.SetSubType("FontStyle");

            FHost.UpdateEnum("CssFontWeight", "100", new string[] { "100 ", "200", "300","400", "500", "600", "700", "800", "900" });
            FHost.CreateEnumInput("FontWeight", TSliceMode.Single, TPinVisibility.True, out FFontWeigthIn);
            FFontWeigthIn.SetSubType("CssFontWeight");

            FHost.UpdateEnum("FontDecoration", "none ", new string[] { "none  ", "underline ", "overline ", "line-through", "blink " });
            FHost.CreateEnumInput("FontDecoration", TSliceMode.Single, TPinVisibility.True, out FTextDecortationIn);
            FTextDecortationIn.SetSubType("FontDecoration");

            FHost.CreateValueInput("WordSpacing", 1, null, TSliceMode.Dynamic, TPinVisibility.OnlyInspector, out FWordSpacingIn);
            FWordSpacingIn.SetSubType(0, double.MaxValue, 0.01, 0, false, false, false);

            FHost.CreateValueInput("LetterSpacing", 1, null, TSliceMode.Dynamic, TPinVisibility.OnlyInspector, out FLetterSpacingIn);
            FLetterSpacingIn.SetSubType(0, double.MaxValue, 0.01, 0, false, false, false);

            FHost.CreateStringInput("FontStretch", TSliceMode.Dynamic, TPinVisibility.OnlyInspector, out FFontStretchIn);
            FFontStretchIn.SetSubType("normal", false);

            
        }

        #endregion pin creation






        #region mainloop


        /// <summary>
        /// nothing to configure in this plugin
        /// only used in conjunction with inputs of WebType cmpdConfigurate
        /// </summary>
        /// <param name="Input"></param>
        protected override void OnConfigurate(IPluginConfig Input)
        {

        }


        /// <summary>
        /// here we go, thats the method called by vvvv each frame
        /// all data handling should be in here
        /// </summary>
        /// <param name="SpreadMax"></param>
        protected override void OnEvaluate(int SpreadMax)
        {

			if (FFontSizeIn.PinIsChanged || FColorInput.PinIsChanged || FFontFamiliyIn.PinIsChanged || FFontStyleIn .PinIsChanged || FFontWeigthIn.PinIsChanged || FTextDecortationIn.PinIsChanged || FWordSpacingIn.PinIsChanged ||FLetterSpacingIn.PinIsChanged)
            {
				// set slices count
                mCssPropertiesOwn.Clear();	
									
                for (int i = 0; i < SpreadMax; i++)
                {

                    double currentFontSizeSlice;
                    RGBAColor currentColorSlice;
                    string currentFontFamily;
                    string currentFontStyle;
                    string currentFontWeight;
                    string currentTextDecoration;
                    double currentWordSpacing;
                    double currentLetterSpacing;
                    string currentFontStretch;
                    SortedList<string, string> tCssProperty = new SortedList<string,string>();
                    
                    // get current values
                    FFontSizeIn.GetValue(i, out currentFontSizeSlice);
					FColorInput.GetColor(i, out currentColorSlice);
                    FFontFamiliyIn.GetString(i,out currentFontFamily);
                    FFontStyleIn.GetString(0,out currentFontStyle);
                    FFontWeigthIn.GetString(0,out currentFontWeight);
                    FTextDecortationIn.GetString(0,out currentTextDecoration);
                    FWordSpacingIn.GetValue(i,out currentWordSpacing);
                    FLetterSpacingIn.GetValue(i,out currentLetterSpacing);
                    FFontStretchIn.GetString(i,out currentFontStretch);

					// add css webattributes



                    tCssProperty.Add("font-size",(double)Math.Round(currentFontSizeSlice * 100,1)+ "%");
                    tCssProperty.Add("font-color", "rgb(" + Math.Round(currentColorSlice.R * 100) + "%," + Math.Round(currentColorSlice.G * 100) + "%," + Math.Round(currentColorSlice.B * 100) + "%)");
                    tCssProperty.Add("font-family", currentFontFamily);
                    tCssProperty.Add("font-weight", currentFontWeight.ToString());
                    tCssProperty.Add("font-style", currentFontStyle);
                    tCssProperty.Add("text-decoration", currentTextDecoration);
                    tCssProperty.Add("word-spacing", Convert.ToString(currentWordSpacing ));
                    tCssProperty.Add("letter-spacing", Convert.ToString(currentLetterSpacing));
                    tCssProperty.Add("font-stretch", currentFontStretch);

                    mCssPropertiesOwn.Add(i, tCssProperty);           
			        
                }
            }
        }
				
        #endregion mainloop
    }
}

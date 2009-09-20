﻿// <copyright file="GEWebBrowser.cs" company="FC">
// Copyright (c) 2008 Fraser Chapman
// </copyright>
// <author>Fraser Chapman</author>
// <email>fraser.chapman@gmail.com</email>
// <date>2008-12-22</date>
// <summary>This file is part of FC.GEPluginCtrls
// FC.GEPluginCtrls is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program. If not, see http://www.gnu.org/licenses/.
// </summary>
namespace FC.GEPluginCtrls
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Windows.Forms;
    using GEPlugin;

    /// <summary>
    /// Main delegate event handler
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public delegate void GEWebBrowserEventHandeler(object sender, GEEventArgs e);

    /// <summary>
    /// The available imagery bases for the plug-in
    /// </summary>
    public enum ImageryBase
    {
        /// <summary>
        /// Earth imagery base
        /// </summary>
        Earth,

        /// <summary>
        /// Mars imagery base
        /// </summary>
        Mars,

        /// <summary>
        /// Moon imagery base
        /// </summary>
        Moon
    }

    /// <summary>
    /// This browser control holds the Google Earth Plug-in,
    /// it also provides wrapper methods to work with the Google.Earth namespace
    /// </summary>
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public partial class GEWebBrowser : WebBrowser
    {
        #region Private fields

        /// <summary>
        /// External is A COM Visible class that holds all the public methods
        /// to be called from javascript. An instance of this is set
        /// to the base object's ObjectForScripting property in the constuctor.
        /// </summary>
        private External external = null;

        /// <summary>
        /// Use the IGEPlugin COM interface. 
        /// Equivalent to QueryInterface for COM objects
        /// </summary>
        private IGEPlugin geplugin = null;

        /// <summary>
        /// Current plug-in imagery database
        /// </summary>
        private ImageryBase imageryBase = ImageryBase.Earth;

        /// <summary>
        /// Indicates whether the plug-in is ready to use
        /// </summary>
        private bool pluginIsReady = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the GEWebBrowser class.
        /// </summary>
        public GEWebBrowser()
            : base()
        {
            // External - COM visible class
            this.external = new External();
            this.external.KmlLoaded += new ExternalEventHandeler(this.External_KmlLoaded);
            this.external.PluginReady += new ExternalEventHandeler(this.External_PluginReady);
            this.external.ScriptError += new ExternalEventHandeler(this.External_ScriptError);
            this.external.KmlEvent += new ExternalEventHandeler(this.External_KmlEvent);
            
            // Setup the desired control defaults
            this.AllowNavigation = false;
            this.IsWebBrowserContextMenuEnabled = false;
            this.ScrollBarsEnabled = false;
            this.ScriptErrorsSuppressed = true;
            this.WebBrowserShortcutsEnabled = false;
            this.ObjectForScripting = this.external;
            this.DocumentCompleted +=
                new WebBrowserDocumentCompletedEventHandler(this.GEWebBrowser_DocumentCompleted);
        }

        #region Public events

        /// <summary>
        /// Raised when the plugin is ready
        /// </summary>
        public event GEWebBrowserEventHandeler PluginReady;

        /// <summary>
        /// Raised when there is a kmlEvent
        /// </summary>
        public event GEWebBrowserEventHandeler KmlEvent;

        /// <summary>
        /// Raised when a kml/kmz file has loaded
        /// </summary>
        public event GEWebBrowserEventHandeler KmlLoaded;

        /// <summary>
        /// Raised when there is a script error in the document 
        /// </summary>
        public event GEWebBrowserEventHandeler ScriptError;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the current imagery base for the plug-in
        /// </summary>
        [Category("Control Options"),
        Description("Gets or sets the current imagery base for the plug-in."),
        DefaultValueAttribute(ImageryBase.Earth)]
        public ImageryBase ImageyBase
        {
            get
            {
                return this.imageryBase;
            }

            set
            {
               this.CreateInstance(this.imageryBase);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the plug-in is ready
        /// </summary>
        public bool PluginIsReady
        {
            get
            {
                return this.pluginIsReady;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Wrapper for the the google.earth.addEventListener method
        /// </summary>
        /// <param name="feature">The target feature</param>
        /// <param name="action">The event Id</param>
        public void AddEventListener(object feature, string action)
        {
            this.InvokeJavascript(
                "jsAddEventListener",
                new object[] { feature, action });
        }

        /// <summary>
        /// Wrapper for the the google.earth.addEventListener method
        /// </summary>
        /// <param name="feature">The target feature</param>
        /// <param name="action">The event Id</param>
        /// <param name="callBackFunction">The callback function to use</param>
        public void AddEventListener(object feature, string action, string callBackFunction)
        {
            this.InvokeJavascript(
                "jsAddEventListener",
                new object[] { feature, action, callBackFunction });
        }

        /// <summary>
        /// Sets the imagery database to use with the plug-in
        /// </summary>
        /// <param name="database">The database name</param>
        public void CreateInstance(ImageryBase database)
        {
            if (this.Document != null)
            {
                string name = Enum.GetName(typeof(ImageryBase), database);
                this.InvokeJavascript(
                    "jsCreateInstance",
                    new string[] { name });
                this.imageryBase = database;
            }
        }

        /// <summary>
        /// Load a kml/kmz file 
        /// This function requires a 'twin' LoadKml function in javascript
        /// this twin function will call "google.earth.fetchKml"
        /// </summary>
        /// <param name="url">path to a kml/kmz file</param>
        public void FetchKml(string url)
        {
            if (this.Document != null)
            {
                this.Document.InvokeScript(
                    "jsFetchKml",
                    new string[] { url });
            }
        }

        /// <summary>
        /// Get the plugin instance associated with the control
        /// </summary>
        /// <returns>The plugin instance</returns>
        public IGEPlugin GetPlugin()
        {
            return this.geplugin;
        }
        
        /// <summary>
        /// Invokes the javascript function 'doGeocode'
        /// Automatically flys to the location if one is found
        /// </summary>
        /// <param name="input">the location to geocode</param>
        /// <returns>the point object (if any)</returns>
        public IKmlPoint InvokeDoGeocode(string input)
        {
            if (this.Document == null)
            {
                return null;
            }

            return (IKmlPoint)this.InvokeJavascript("jsDoGeocode", new object[] { input });
        }

        /// <summary>
        /// Inject a javascript element into the document head
        /// </summary>
        /// <param name="javascript">the script code</param>
        public void InjectJavascript(string javascript)
        {
            if (this.Document != null)
            {
                try
                {
                    HtmlElement headElement = this.Document.GetElementsByTagName("head")[0];
                    HtmlElement scriptElement = this.Document.CreateElement("script");
                    scriptElement.SetAttribute("type", "text/javascript");

                    // use the custom mshtml interface to append the script to the element
                    IHTMLScriptElement element = (IHTMLScriptElement)scriptElement.DomElement;
                    element.Text = "/* <![CDATA[ */ " + javascript + " /* ]]> */";
                    headElement.AppendChild(scriptElement);
                }
                catch (InvalidOperationException ioex)
                {
                    System.Diagnostics.Debug.WriteLine(ioex.ToString());
                    this.OnScriptError(this, new GEEventArgs(ioex.Message, ioex.ToString()));
                }
            }
        }

        /// <summary>
        /// Executes a script function defined in the currently loaded document. 
        /// </summary>
        /// <param name="function">The name of the function to invoke</param>
        /// <returns>The result of the evaluated function</returns>
        public object InvokeJavascript(string function)
        {
            return this.InvokeJavascript(function, new object[] { });
        }

        /// <summary>
        /// Executes a script function that is defined in the currently loaded document. 
        /// </summary>
        /// <param name="function">The name of the function to invoke</param>
        /// <param name="args">any arguments</param>
        /// <returns>The result of the evaluated function</returns>
        public object InvokeJavascript(string function, object[] args)
        {
            if (this.Document != null)
            {
                // see http://msdn.microsoft.com/en-us/library/4b1a88bz.aspx
                return this.Document.InvokeScript(function, args);
            }
            else
            {
                return new object { };
            }
        }

        /// <summary>
        /// Kills all running geplugin processes on the system
        /// </summary>
        public void KillAllPluginProcesses()
        {
            try
            {
                // find all the running 'geplugin' processes 
                System.Diagnostics.Process[] gep =
                    System.Diagnostics.Process.GetProcessesByName("geplugin");

                // whilst there are matching processes
                while (gep.Length > 0)
                {
                    // terminate them
                    gep[gep.Length - 1].Kill();
                }
            }
            catch (InvalidOperationException ioex)
            {
                System.Diagnostics.Debug.WriteLine(ioex.ToString());
            }
        }

        /// <summary>
        /// Load the embeded html document into the browser 
        /// </summary>
        public void LoadEmbededPlugin()
        {
            try
            {
                // Get the html string from the embebed reasource
                string html = Properties.Resources.Plugin;

                // Create a temp file and get the full path
                string path = Path.GetTempFileName();

                // Write the html to the temp file
                TextWriter tw = new StreamWriter(path);
                tw.Write(html);

                // Close the temp file
                tw.Close();

                // Navigate to the temp file
                this.Navigate(path);

                // NB: Windows deletes the temp file automatially when the Windows session quits.
            }
            catch (IOException ioex)
            {
                System.Diagnostics.Debug.WriteLine(ioex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Wrapper for the the google.earth.removeEventListener method
        /// </summary>
        /// <param name="feature">The target feature</param>
        /// <param name="action">The event Id</param>
        public void RemoveEventListener(object feature, string action)
        {
            this.InvokeJavascript(
                "jsRemoveEventListner",
                new object[] { feature, action });
        }

        /// <summary>
        /// Take a 'screen grab' of the current GEWebBrowser view
        /// </summary>
        /// <returns>bitmap image</returns>
        public Bitmap ScreenGrab()
        {
            try
            {
                // create a drawing object based on the webbrowser control
                Rectangle rectangle = this.DisplayRectangle;
                Bitmap bitmap =
                    new Bitmap(
                        rectangle.Width,
                        rectangle.Height,
                        PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(bitmap);
                Point point = new Point();

                // copy the current display as a bitmap
                graphics.CopyFromScreen(
                    this.PointToScreen(point),
                    point,
                    new Size(rectangle.Width, rectangle.Height));
                graphics.Dispose();

                return bitmap;
            }
            catch (ArgumentNullException anex)
            {
                System.Diagnostics.Debug.WriteLine(anex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Reloads the document currently displayed in the control
        /// </summary>
        public override void Refresh()
        {
            this.pluginIsReady = false;
            base.Refresh();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Protected method for raising the PluginReady event
        /// </summary>
        /// <param name="plugin">The plugin object</param>
        /// <param name="e">Event arguments</param>
        protected virtual void OnPluginReady(object plugin, GEEventArgs e)
        {
            if (this.PluginReady != null)
            {
                this.PluginReady(plugin, e);
            }
        }

        /// <summary>
        /// Protected method for raising the KmlEvent event
        /// </summary>
        /// <param name="kmlEvent">the kml event</param>
        /// <param name="e">The eventid</param>
        protected virtual void OnKmlEvent(object kmlEvent, GEEventArgs e)
        {
            if (this.KmlEvent != null)
            {
                this.KmlEvent(kmlEvent, e);
            }
        }

        /// <summary>
        /// Protected method for raising the KmlLoaded event
        /// </summary>
        /// <param name="kmlObject">The kmlObject object</param>
        /// <param name="e">Event arguments</param>
        protected virtual void OnKmlLoaded(object kmlObject, GEEventArgs e)
        {
            if (this.KmlLoaded != null)
            {
                this.KmlLoaded(kmlObject, e);
            }
        }

        /// <summary>
        /// Protected method for raising the ScriptError event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event arguments</param>
        protected virtual void OnScriptError(object sender, GEEventArgs e)
        {
            if (this.ScriptError != null)
            {
                this.ScriptError(sender, e);
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Called when the document has a ScriptError
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event arguments</param>
        private void External_ScriptError(object sender, GEEventArgs e)
        {
            this.OnScriptError(sender, e);
        }

        /// <summary>
        /// Called when the Plugin is Ready 
        /// </summary>
        /// <param name="plugin">The plugin instance</param>
        /// <param name="e">Event arguments</param>
        private void External_PluginReady(object plugin, GEEventArgs e)
        {
            this.geplugin = plugin as IGEPlugin;

            try
            {
                // The data is just the version info
                e.Message = "ApiVersion";          
                e.Data = this.geplugin.getApiVersion();
            }
            catch (InvalidOperationException ioex)
            {
                // Possible problems with the interop assemby version and installed plug-in version
                GEEventArgs exea = new GEEventArgs("InvalidOperationException", ioex.ToString());
                System.Diagnostics.Debug.WriteLine(ioex.ToString());
                this.OnScriptError(this.geplugin, e);
            }
            catch (AccessViolationException avex)
            {
                // Possible problems with the interop assemby version and installed plug-in version
                GEEventArgs exea = new GEEventArgs("AccessViolationException", avex.ToString());
                System.Diagnostics.Debug.WriteLine(avex.ToString());
                this.OnScriptError(this.geplugin, exea);
            }
            finally
            {
                this.pluginIsReady = true;

                // Raise the ready event
                this.OnPluginReady(this.geplugin, e);
            }
        }

        /// <summary>
        /// Called when there is a Kml event 
        /// </summary>
        /// <param name="kmlEvent">the kml event</param>
        /// <param name="e">The eventId</param>
        private void External_KmlEvent(object kmlEvent, GEEventArgs e)
        {
            this.OnKmlEvent(kmlEvent, e);
        }

        /// <summary>
        /// Called when a Kml/Kmz file has loaded
        /// </summary>
        /// <param name="kmlFeature">The kml feature</param>
        /// <param name="e">Event arguments</param>
        private void External_KmlLoaded(object kmlFeature, GEEventArgs e)
        {
            this.OnKmlLoaded(kmlFeature, e);
        }

        /// <summary>
        /// Called when the Html document has finished loading
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event arguments</param>
        private void GEWebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // Set up the error handler for a loaded Document
            this.Document.Window.Error += new HtmlElementErrorEventHandler(this.Window_Error);
        }

        /// <summary>
        /// Called when there is a script error in the window
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">Event arguments</param>
        private void Window_Error(object sender, HtmlElementErrorEventArgs e)
        {
            // Handle the original error
            e.Handled = true;

            // Build the error data
            GEEventArgs ea = new GEEventArgs();
            ea.Message = "line " + e.LineNumber.ToString() + " - " + e.Description;
            ea.Data = "Document Error";

            // Bubble the error
            this.OnScriptError(e.ToString(), ea);
        }

        #endregion
    }
}

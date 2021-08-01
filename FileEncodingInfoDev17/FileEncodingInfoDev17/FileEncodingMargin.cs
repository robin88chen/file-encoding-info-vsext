using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using System.IO;

namespace FileEncodingInfoDev17
{
    /// <summary>
    /// Margin's canvas and visual definition including both size and content
    /// </summary>
    internal class FileEncodingMargin : Canvas, IWpfTextViewMargin
    {
        /// <summary>
        /// Margin name.
        /// </summary>
        public const string MarginName = "FileEncodingMargin";

        /// <summary>
        /// A value indicating whether the object is disposed.
        /// </summary>
        private bool isDisposed;

        private readonly FileEncodingMarginFactory m_factory;
        private readonly IWpfTextView m_textView;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEncodingMargin"/> class for a given <paramref name="textView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> to attach the margin to.</param>
        public FileEncodingMargin(IWpfTextView textView, IWpfTextViewHost host, FileEncodingMarginFactory factory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            m_factory = factory;
            m_textView = textView;
            uint co = 0xff000000;
            uint text_co = 0xffffffff;
            IVsUIShell5 ui_shell = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell5;
            if (ui_shell != null)
            {
                co = ui_shell.GetThemedColor(new System.Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"),
                    "ScrollBar", 0);
                text_co = ui_shell.GetThemedColor(new System.Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"),
                    "ButtonText", 0);
            }
            ITextDocument doc = null;
            if (m_factory.TextDocumentFactoryService != null)
            {
                m_factory.TextDocumentFactoryService.TryGetTextDocument(m_textView.TextBuffer, out doc);
            }
            string path_and_encoding = "";
            if (doc != null)
            {
                string sr_encoding_name = doc.Encoding.EncodingName;
                try
                {
                    using (FileStream fs = File.OpenRead(doc.FilePath))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            sr.Read();
                            sr_encoding_name = sr.CurrentEncoding.EncodingName;
                            sr.Close();
                        }
                        fs.Close();
                    }
                }
                catch
                {

                }
                path_and_encoding = $"{doc.FilePath} -- {sr_encoding_name}";
            }
            path_and_encoding = path_and_encoding.Replace("_", "__");
            byte a = (byte)(co >> 24);
            byte b = (byte)((co >> 16) & 0xff);
            byte g = (byte)((co >> 8) & 0xff);
            byte r = (byte)(co & 0xff);
            byte ta = (byte)(text_co >> 24);
            byte tb = (byte)((text_co >> 16) & 0xff);
            byte tg = (byte)((text_co >> 8) & 0xff);
            byte tr = (byte)(text_co & 0xff);
            Brush backgroundBrush = new SolidColorBrush(new Color() { A = a, R = r, G = g, B = b }); // font.BackgroundBrush;
            Brush foregroundBrush = new SolidColorBrush(new Color() { A = ta, R = tr, G = tg, B = tb }); //font.ForegroundBrush;

            base.Background = backgroundBrush;

            this.Height = 20; // Margin height sufficient to have the label
            this.ClipToBounds = true;
            this.Background = backgroundBrush;
            //this.Background = new SolidColorBrush(Colors.LightGreen);

            // Add a green colored label that says "Hello EncodingInfoMargin"
            var scroll = m_textView.ViewScroller;
            var btn = new Button();
            var label = new Label
            {
                Background = backgroundBrush, // new SolidColorBrush(Colors.LightGreen),
                Content = path_and_encoding,
                Foreground = foregroundBrush
            };

            this.Children.Add(label);
        }

        #region IWpfTextViewMargin

        /// <summary>
        /// Gets the <see cref="Sytem.Windows.FrameworkElement"/> that implements the visual representation of the margin.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                this.ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin

        /// <summary>
        /// Gets the size of the margin.
        /// </summary>
        /// <remarks>
        /// For a horizontal margin this is the height of the margin,
        /// since the width will be determined by the <see cref="ITextView"/>.
        /// For a vertical margin this is the width of the margin,
        /// since the height will be determined by the <see cref="ITextView"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();

                // Since this is a horizontal margin, its width will be bound to the width of the text view.
                // Therefore, its size is its height.
                return this.ActualHeight;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the margin is enabled.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public bool Enabled
        {
            get
            {
                this.ThrowIfDisposed();

                // The margin should always be enabled
                return true;
            }
        }

        /// <summary>
        /// Gets the <see cref="ITextViewMargin"/> with the given <paramref name="marginName"/> or null if no match is found
        /// </summary>
        /// <param name="marginName">The name of the <see cref="ITextViewMargin"/></param>
        /// <returns>The <see cref="ITextViewMargin"/> named <paramref name="marginName"/>, or null if no match is found.</returns>
        /// <remarks>
        /// A margin returns itself if it is passed its own name. If the name does not match and it is a container margin, it
        /// forwards the call to its children. Margin name comparisons are case-insensitive.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="marginName"/> is null.</exception>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, FileEncodingMargin.MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        /// <summary>
        /// Disposes an instance of <see cref="FileEncodingMargin"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Checks and throws <see cref="ObjectDisposedException"/> if the object is disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(MarginName);
            }
        }
    }
}

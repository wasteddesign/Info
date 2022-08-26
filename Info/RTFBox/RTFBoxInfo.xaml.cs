using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using ColorPicker;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;

namespace WDE.Info
{
    /// <summary>
    /// Interaktionslogik für "RTFBox.xaml"
    /// </summary>
    public partial class RTFBoxInfo 
    {
        /// <summary>
        /// Konstruktor - initialisiert alle graphischen Komponenten
        /// </summary>
        public RTFBoxInfo()
        {   
            this.InitializeComponent();
  
        }

        internal IntPtr Handle { get; private set; }

        public RichTextBox GetRichTextBox()
        {
            return RichTextControl;
        }

        internal void ToolBarLoaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }
       
        #region Variablen

        private bool dataChanged = false; // ungespeicherte Textänderungen     

        private string privateText = null; // Inhalt der RTFBox im txt-Format
        public string text
        {
            get
            {
                TextRange range = new TextRange(RichTextControl.Document.ContentStart, RichTextControl.Document.ContentEnd);
                return range.Text;
            }
            set
            {
                privateText = value;
            }
        }

        private string zeilenangabe; // aktuelle Zeile der Cursorposition
        private int privAktZeile = 1; 
        public int aktZeile
        {
            get { return privAktZeile; }
            set 
            { 
                privAktZeile = value;
                zeilenangabe = "Line: " + value;
                LabelZeileNr.Content = zeilenangabe;
            }
        }

        private string spaltenangabe; // aktuelle Spalte der Cursorposition
        private int privAktSpalte = 1; 
        public int aktSpalte
        {
            get { return privAktSpalte; }
            set 
            { 
                privAktSpalte = value;
                spaltenangabe = "Column: " + value;
                LabelSpalteNr.Content = spaltenangabe;
            }
        }
       
        #endregion Variablen     

        #region ControlHandler

        //
        // Nach dem Laden des Control
        //
        private void RTFEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // Schrifttypen- und -größen-Initialisierung
            TextRange range = new TextRange(RichTextControl.Selection.Start, RichTextControl.Selection.End);
            Fonttype.SelectedValue = range.GetPropertyValue(FlowDocument.FontFamilyProperty).ToString();
            Fontheight.SelectedValue = range.GetPropertyValue(FlowDocument.FontSizeProperty).ToString();

            // aktuelle Zeilen- und Spaltenpositionen angeben
            aktZeile = Zeilennummer();
            aktSpalte = Spaltennummer();           

            RichTextControl.Focus();
        }


        #endregion ControlHandler

        #region private ToolBarHandler

        //
        // ToolStripButton Open wurde gedrückt
        //
        private void ToolStripButtonOpen_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            SetRTF("");


            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".rtf"; // Default file extension
            dlg.Filter = "RichText documents (.rtf)|*.rtf|Text documents (.txt)|*.txt"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {                
                string path = dlg.FileName;

                // Open document
                TextRange range;
                FileStream fStream;

                if (File.Exists(path))
                {
                    range = new TextRange(RichTextControl.Document.ContentStart, RichTextControl.Document.ContentEnd);
                    fStream = new FileStream(path, FileMode.OpenOrCreate);
                    range.Load(fStream, DataFormats.Rtf);
                    fStream.Close();
                }
            }			
		}

        //
        // ToolStripButton Print wurde gedrückt
        //
		private void ToolStripButtonPrint_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            // Configure printer dialog box
            PrintDialog dlg = new PrintDialog();
            dlg.PageRangeSelection = PageRangeSelection.AllPages;
            dlg.UserPageRangeEnabled = true;            

            // Show and process save file dialog box results
            if (dlg.ShowDialog() == true)
            {
                //use either one of the below    
                // dlg.PrintVisual(RichTextControl as Visual, "printing as visual");
                dlg.PrintDocument((((IDocumentPaginatorSource)RichTextControl.Document).DocumentPaginator), "printing as paginator");
            }
		}

        //
        // ToolStripButton Strikeout wurde gedrückt
        // (läßt sich nicht durch Command in XAML abarbeiten)
        //
        private void ToolStripButtonStrikeout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Ereignishandlerimplementierung hier einfügen.
            TextRange range = new TextRange(RichTextControl.Selection.Start, RichTextControl.Selection.End);

            TextDecorationCollection tdc = (TextDecorationCollection)RichTextControl.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            if (tdc == null || !tdc.Equals(TextDecorations.Strikethrough))
            {
                tdc = TextDecorations.Strikethrough;

            }
            else
            {
                tdc = new TextDecorationCollection();
            }
            range.ApplyPropertyValue(Inline.TextDecorationsProperty, tdc);
        }

        //
        // ToolStripButton Textcolor wurde gedrückt
        //
        private void ToolStripButtonTextcolor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            //colorDialog.Owner = this;
            if ((bool)colorDialog.ShowDialog())
            {
                TextRange range = new TextRange(RichTextControl.Selection.Start, RichTextControl.Selection.End);

                range.ApplyPropertyValue(FlowDocument.ForegroundProperty, new SolidColorBrush(colorDialog.SelectedColor));                
            }
        }

        //
        // ToolStripButton Backgroundcolor wurde gedrückt
        //
        private void ToolStripButtonBackcolor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            //colorDialog.Owner = this;
            if ((bool)colorDialog.ShowDialog())
            {
                TextRange range = new TextRange(RichTextControl.Selection.Start, RichTextControl.Selection.End);

                range.ApplyPropertyValue(FlowDocument.BackgroundProperty, new SolidColorBrush(colorDialog.SelectedColor));
            }
        }

        //
        // ToolStripButton Subscript wurde gedrückt
        // (läßt sich auch durch Command in XAML machen. 
        // Damit dann aber ein wirkliches Subscript angezeigt wird, muß die benutzte Font OpenType sein:
        // http://msdn.microsoft.com/en-us/library/ms745109.aspx#variants
        // Um auch für alle anderen Schriftarten Subscript zu realisieren, läßt sich stattdessen die Baseline Eigenschaft verändern)
        //
        private void ToolStripButtonSubscript_Click(object sender, RoutedEventArgs e)
        {
            var currentAlignment = RichTextControl.Selection.GetPropertyValue(Inline.BaselineAlignmentProperty);

            BaselineAlignment newAlignment = ((BaselineAlignment)currentAlignment == BaselineAlignment.Subscript) ? BaselineAlignment.Baseline : BaselineAlignment.Subscript;
            RichTextControl.Selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, newAlignment);
        }

        //
        // ToolStripButton Superscript wurde gedrückt
        // (läßt sich auch durch Command in XAML machen. 
        // Damit dann aber ein wirkliches Superscript angezeigt wird, muß die benutzte Font OpenType sein:
        // http://msdn.microsoft.com/en-us/library/ms745109.aspx#variants
        // Um auch für alle anderen Schriftarten Superscript zu realisieren, läßt sich stattdessen die Baseline Eigenschaft verändern)
        //
        private void ToolStripButtonSuperscript_Click(object sender, RoutedEventArgs e)
        { 
	        var currentAlignment = RichTextControl.Selection.GetPropertyValue(Inline.BaselineAlignmentProperty);
    	 
	        BaselineAlignment newAlignment = ((BaselineAlignment)currentAlignment == BaselineAlignment.Superscript) ? BaselineAlignment.Baseline : BaselineAlignment.Superscript;
	        RichTextControl.Selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, newAlignment);
        }

        //
        // Textgröße wurde vom Benutzer verändert
        //
        private void Fontheight_DropDownClosed(object sender, EventArgs e)
        {
            string fontHeight = (string)Fontheight.SelectedItem;

            if (fontHeight != null)
            {                
                RichTextControl.Selection.ApplyPropertyValue(System.Windows.Controls.RichTextBox.FontSizeProperty, fontHeight);
                RichTextControl.Focus();
            }
        }

        //
        // andere Font wurde vom Benutzer ausgewählt
        //
        private void Fonttype_DropDownClosed(object sender, EventArgs e)
        {            
            string fontName = (string)Fonttype.SelectedItem;

            if (fontName != null)
            {                
                RichTextControl.Selection.ApplyPropertyValue(System.Windows.Controls.RichTextBox.FontFamilyProperty, fontName);
                RichTextControl.Focus();
            }
        }

        //
        // Alignmentstatus anpassen
        //
        private void ToolStripButtonAlignLeft_Click(object sender, RoutedEventArgs e)
        {
            if (ToolStripButtonAlignLeft.IsChecked == true)
            {
                ToolStripButtonAlignCenter.IsChecked = false;
                ToolStripButtonAlignRight.IsChecked = false;
            }
        }

        //
        // Alignmentstatus anpassen
        //
        private void ToolStripButtonAlignCenter_Click(object sender, RoutedEventArgs e)
        {
            if (ToolStripButtonAlignCenter.IsChecked == true)
            {
                ToolStripButtonAlignLeft.IsChecked = false;
                ToolStripButtonAlignRight.IsChecked = false;
            }

        }

        //
        // Alignmentstatus anpassen
        //
        private void ToolStripButtonAlignRight_Click(object sender, RoutedEventArgs e)
        {
            if (ToolStripButtonAlignRight.IsChecked == true)
            {
                ToolStripButtonAlignCenter.IsChecked = false;
                ToolStripButtonAlignLeft.IsChecked = false;
            }

        }

        #endregion private ToolBarHandler
        
        #region private RichTextBoxHandler
        
        //
        // Formatierungen des markierten Textes anzeigen
        //
        private void RichTextControl_SelectionChanged(object sender, RoutedEventArgs e)
        {     
            // markierten Text holen
            TextRange selectionRange = new TextRange(RichTextControl.Selection.Start, RichTextControl.Selection.End);
            
            
            if (selectionRange.GetPropertyValue(FontWeightProperty).ToString() == "Bold")
            {
                ToolStripButtonBold.IsChecked = true;
            }
            else
            {
                ToolStripButtonBold.IsChecked = false;
            }

            if (selectionRange.GetPropertyValue(FontStyleProperty).ToString() == "Italic")
            {
                ToolStripButtonItalic.IsChecked = true;
            }
            else
            {
                ToolStripButtonItalic.IsChecked = false;
            }

            if (selectionRange.GetPropertyValue(Inline.TextDecorationsProperty) == TextDecorations.Underline)
            {
                ToolStripButtonUnderline.IsChecked = true;
            }
            else
            {
                ToolStripButtonUnderline.IsChecked = false;
            }

            if (selectionRange.GetPropertyValue(Inline.TextDecorationsProperty) == TextDecorations.Strikethrough)
            {
                ToolStripButtonStrikeout.IsChecked = true;
            }
            else
            {
                ToolStripButtonStrikeout.IsChecked = false;
            } 

            if (selectionRange.GetPropertyValue(FlowDocument.TextAlignmentProperty).ToString() == "Left")
            {
                ToolStripButtonAlignLeft.IsChecked = true;
            }

            if (selectionRange.GetPropertyValue(FlowDocument.TextAlignmentProperty).ToString() == "Center")
            {
                ToolStripButtonAlignCenter.IsChecked = true;
            }

            if (selectionRange.GetPropertyValue(FlowDocument.TextAlignmentProperty).ToString() == "Right")
            {
                ToolStripButtonAlignRight.IsChecked = true;
            }
            
            // Sub-, Superscript Buttons setzen
            try
            {                
                switch ((BaselineAlignment)selectionRange.GetPropertyValue(Inline.BaselineAlignmentProperty))
                {
                    case BaselineAlignment.Subscript:
                        ToolStripButtonSubscript.IsChecked = true;
                        ToolStripButtonSuperscript.IsChecked = false;
                        break;

                    case BaselineAlignment.Superscript:
                        ToolStripButtonSubscript.IsChecked = false;
                        ToolStripButtonSuperscript.IsChecked = true;
                        break;

                    default:
                        ToolStripButtonSubscript.IsChecked = false;
                        ToolStripButtonSuperscript.IsChecked = false;
                        break;
                }
            }
            catch (Exception) 
            {
                ToolStripButtonSubscript.IsChecked = false;
                ToolStripButtonSuperscript.IsChecked = false;
            }                    

            // Get selected font and height and update selection in ComboBoxes
            Fonttype.SelectedValue = selectionRange.GetPropertyValue(FlowDocument.FontFamilyProperty).ToString();
            Fontheight.SelectedValue = selectionRange.GetPropertyValue(FlowDocument.FontSizeProperty).ToString();

            // Ausgabe der Zeilennummer
            aktZeile = Zeilennummer();            

            // Ausgabe der Spaltennummer
            aktSpalte = Spaltennummer(); 
        }              

        //
        // wurden Textänderungen gemacht
        //
        private void RichTextControl_TextChanged(object sender, TextChangedEventArgs e)
        {
            dataChanged = true;

            foreach (var block in RichTextControl.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    foreach (var inline in ((Paragraph)block).Inlines)
                    {
                        if (inline is InlineUIContainer)
                        {
                            Image img = (((InlineUIContainer)inline).Child as Image);

                            img.Loaded += (sender2, e2) =>
                            {
                                AdornerLayer al = AdornerLayer.GetAdornerLayer(img);
                                if (al != null)
                                {
                                    
                                    al.Add(new ResizingAdorner(img));
                                }
                            };
                        }
                    }
                }
            }
        }

        //
        // Tastendruck erzeugt ein neues Zeichen in der gewählten Font
        //
        private void RichTextControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            dataChanged = true;

            string fontName = (string)Fonttype.SelectedValue;
            string fontHeight = (string)Fontheight.SelectedValue;
            TextRange range = new TextRange(RichTextControl.Selection.Start, RichTextControl.Selection.End);

            range.ApplyPropertyValue(TextElement.FontFamilyProperty, fontName);
            range.ApplyPropertyValue(TextElement.FontSizeProperty, fontHeight);
            Check_Link(sender, e);
        }

        private void Check_Link(object sender, KeyEventArgs e)
        {
            var rtb = (RichTextBox)sender;
            if (e.Key != Key.Space && e.Key != Key.Return) return;

            var caretPosition = rtb.Selection.Start;
            TextPointer wordStartPosition;

            var word = GetPreceedingWordInParagraph(caretPosition, out wordStartPosition);
            if (!Uri.IsWellFormedUriString(word, UriKind.Absolute)) return;

            if (wordStartPosition == null || caretPosition == null) return;

            var tpStart = wordStartPosition.GetPositionAtOffset(0, LogicalDirection.Backward);
            var tpEnd = caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);

            if (tpStart != null && tpEnd != null)
            {
                var link = new Hyperlink(tpStart, tpEnd)
                {
                    NavigateUri = new Uri(word)
                };

                link.MouseLeftButtonDown += FollowHyperlink;
            }
        }
    

        private static string GetPreceedingWordInParagraph(TextPointer position, out TextPointer wordStartPosition)
        {
            wordStartPosition = null;
            var word = String.Empty;
            var paragraph = position.Paragraph;

            if (paragraph != null)
            {
                var navigator = position;
                while (navigator != null && navigator.CompareTo(paragraph.ContentStart) > 0)
                {
                    var runText = navigator.GetTextInRun(LogicalDirection.Backward);

                    if (runText.Contains(" "))
                    {
                        var index = runText.LastIndexOf(" ", StringComparison.Ordinal);
                        word = runText.Substring(index + 1, runText.Length - index - 1) + word;
                        wordStartPosition = navigator.GetPositionAtOffset(-1 * (runText.Length - index - 1));
                        break;
                    }

                    wordStartPosition = navigator;
                    word = runText + word;
                    navigator = navigator.GetNextContextPosition(LogicalDirection.Backward);
                }
            }

            return word;
        }

        private static void FollowHyperlink(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;

            var link = (Hyperlink)sender;
            Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()));
            e.Handled = true;
        }

        //
        // Tastenkombinationen auswerten
        //
        private void RichTextControl_KeyUp(object sender, KeyEventArgs e)
        {
            // Ctrl + B
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.B))
            {
                if (ToolStripButtonBold.IsChecked == true)
                {
                    ToolStripButtonBold.IsChecked = false;
                }
                else
                {
                    ToolStripButtonBold.IsChecked = true;
                }
            }

            // Ctrl + I
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.I))
            {
                if (ToolStripButtonItalic.IsChecked == true)
                {
                    ToolStripButtonItalic.IsChecked = false;
                }
                else
                {
                    ToolStripButtonItalic.IsChecked = true;
                }
            }

            // Ctrl + U
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.U))
            {
                if (ToolStripButtonUnderline.IsChecked == true)
                {
                    ToolStripButtonUnderline.IsChecked = false;
                }
                else
                {
                    ToolStripButtonUnderline.IsChecked = true;
                }
            }

            // Ctrl + O
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.O))
            {
                ToolStripButtonOpen_Click(sender, e);
            }
        }

        #endregion private RichTextBoxHandler

        #region private Funktionen

        //
        // Gibt die Zeilennummer der aktuellen Cursorposition zurück
        //
        private int Zeilennummer()
        {
            TextPointer caretLineStart = RichTextControl.CaretPosition.GetLineStartPosition(0);
            TextPointer p = RichTextControl.Document.ContentStart.GetLineStartPosition(0);
            int currentLineNumber = 1;
            
            // Vom Anfang des RTF-Box Inhaltes wird vorwärts solange weitergezählt, bis die aktuelle Cursorposition erreicht ist.
            while (true)
            {
                if (caretLineStart.CompareTo(p) < 0)
                {
                    break;
                }
                int result;
                p = p.GetLineStartPosition(1, out result);
                if (result == 0)
                {
                    break;
                }
                currentLineNumber++;
            }
            return currentLineNumber;
        }

        //
        // Gibt die Spaltennummer der aktuellen Cursorposition zurück
        private int Spaltennummer()
        {
            TextPointer caretPos = RichTextControl.CaretPosition;
            TextPointer p = RichTextControl.CaretPosition.GetLineStartPosition(0);
            int currentColumnNumber = Math.Max(p.GetOffsetToPosition(caretPos) - 1, 0);

            return currentColumnNumber;
        }  
        
        #endregion private Funktionen

        # region öffentliche Funktionen

        //
        // Alle Daten löschen
        //
        public void Clear()
        {            
            dataChanged = false;            
            RichTextControl.Document.Blocks.Clear();            
        }

        //
        // Inhalt der RichTextBox als RTF setzen
        //
        public void SetRTF(string rtf)
        {
            TextRange range = new TextRange(RichTextControl.Document.ContentStart, RichTextControl.Document.ContentEnd);

            // Exception abfangen für StreamReader und MemoryStream, ArgumentException abfangen für range.Load bei rtf=null oder rtf=""          
            try
            {
                // um die Load Methode eines TextRange Objektes zu benutzen wird ein MemoryStream Objekt erzeugt
                using (MemoryStream rtfMemoryStream = new MemoryStream())
                {
                    using (StreamWriter rtfStreamWriter = new StreamWriter(rtfMemoryStream))
                    {
                        rtfStreamWriter.Write(rtf);
                        rtfStreamWriter.Flush();
                        rtfMemoryStream.Seek(0, SeekOrigin.Begin);

                        range.Load(rtfMemoryStream, DataFormats.Rtf);
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }

        //
        // RTF Inhalt der RichTextBox als RTF-String zurückgeben
        //
        public string GetRTF()
        {
            TextRange range = new TextRange(RichTextControl.Document.ContentStart, RichTextControl.Document.ContentEnd);

            // Exception abfangen für StreamReader und MemoryStream
            try
            {
                // um die Load Methode eines TextRange Objektes zu benutzen wird ein MemoryStream Objekt erzeugt
                using (MemoryStream rtfMemoryStream = new MemoryStream())
                {
                    using (StreamWriter rtfStreamWriter = new StreamWriter(rtfMemoryStream))
                    {
                        range.Save(rtfMemoryStream, DataFormats.Rtf);

                        rtfMemoryStream.Flush();
                        rtfMemoryStream.Position = 0;
                        StreamReader sr = new StreamReader(rtfMemoryStream);
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                throw;                
            }
        } 

        #endregion öffentliche Funktionen
        
        #region TODO

        private float zoomFaktor = 1.0F; // Zoom Faktor der RTFBox

        // für das Scrollen bei Drag&Drop Operationen
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        //private const long WM_USER = &H400;
        private const long WM_USER = 1024;

        private void SliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //SendMessage(hwnd, EM_SETZOOM, 10, 10);

            //int result = SendMessage(Handle, (int)WM_USER + 255 , (int)e.NewValue*10, 10);

        }

        // In der StatusBar Ausgabe von Grossschreibweise, Num Lock, Zoom

        #endregion TODO

        private void ToolStripButtonOpenImage_Click(object sender, RoutedEventArgs e)
        {
            var rtc = RichTextControl;
            var image = SelectImage();
            if (image == null) return;

            image.Stretch = Stretch.Fill;

            var tp = rtc.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
            new InlineUIContainer(image, tp);

            //BlockUIContainer container = new BlockUIContainer(image);
            //rtc.Document.Blocks.Add(container);
            //image.Loaded += (sender2, e2) =>
            //{
            //    AdornerLayer al = AdornerLayer.GetAdornerLayer(image);
            //    if (al != null)
            //    {
            //        al.Add(new ResizingAdorner(image));
            //    }
            //};
        }

        private static Image SelectImage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.gif"
            };

            var result = dlg.ShowDialog();
            if (result.Value)
            {
                var bitmap = new BitmapImage(new Uri(dlg.FileName));
                return new Image
                {
                    Source = bitmap,
                    Height = bitmap.Height,
                    Width = bitmap.Width
                };
            }

            return null;
        }

        private void ToolStripButtonPageColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            //colorDialog.Owner = this;
            if ((bool)colorDialog.ShowDialog())
            {   
                RichTextControl.Background = new SolidColorBrush(colorDialog.SelectedColor);
            }
        }

        private void CheckboxSpellCheck_Click(object sender, RoutedEventArgs e)
        {
            RichTextControl.SpellCheck.IsEnabled = !RichTextControl.SpellCheck.IsEnabled;
        }
    }

    public class ResizingAdorner : Adorner
    {
        // Resizing adorner uses Thumbs for visual elements.   
        // The Thumbs have built-in mouse input handling. 
        Thumb topLeft, topRight, bottomLeft, bottomRight;

        double aspect = 1;
        //private double origHeight;
        //private double origWidth;

        // To store and manage the adorner's visual children. 
        VisualCollection visualChildren;

        // Initialize the ResizingAdorner. 
        public ResizingAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            visualChildren = new VisualCollection(this);

            FrameworkElement feae = AdornedElement as FrameworkElement;
            aspect = feae.Height / feae.Width;
            //origHeight = feae.Height;
            //origWidth = feae.Width;

            //adornedElement.MouseLeftButtonDown += (sender, e) =>
            //{
            //    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
            //    {
            //        feae.Width = origWidth;
            //        feae.Height = origHeight;
            //    }
            //};


            // Call a helper method to initialize the Thumbs 
            // with a customized cursors. 
            BuildAdornerCorner(ref topLeft, Cursors.SizeNWSE);
            BuildAdornerCorner(ref topRight, Cursors.SizeNESW);
            BuildAdornerCorner(ref bottomLeft, Cursors.SizeNESW);
            BuildAdornerCorner(ref bottomRight, Cursors.SizeNWSE);

            // Add handlers for resizing. 
            bottomLeft.DragDelta += new DragDeltaEventHandler(HandleBottomLeft);
            bottomRight.DragDelta += new DragDeltaEventHandler(HandleBottomRight);
            topLeft.DragDelta += new DragDeltaEventHandler(HandleTopLeft);
            topRight.DragDelta += new DragDeltaEventHandler(HandleTopRight);
        }

        // Handler for resizing from the bottom-right. 
        void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize. 
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger  
            // than the width or height of an adorner, respectively. 
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            //
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                adornedElement.Height = adornedElement.Width * aspect;
            }
            else
            {
                adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);
            }
        }

        // Handler for resizing from the bottom-left. 
        void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;

            // Ensure that the Width and Height are properly initialized after the resize. 
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger  
            // than the width or height of an adorner, respectively. 
            adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) &&  !Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                adornedElement.Height = adornedElement.Width * aspect;
            }
            else
            {
                adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);
            }
        }

        // Handler for resizing from the top-right. 
        void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;
            FrameworkElement parentElement = adornedElement.Parent as FrameworkElement;

            // Ensure that the Width and Height are properly initialized after the resize. 
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger  
            // than the width or height of an adorner, respectively. 
            adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                adornedElement.Height = adornedElement.Width * aspect;
            }
            else
            {
                adornedElement.Height = Math.Max(args.VerticalChange / 4 + adornedElement.Height, hitThumb.DesiredSize.Height);
            }
        }

        // Handler for resizing from the top-left. 
        void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            FrameworkElement adornedElement = AdornedElement as FrameworkElement;
            Thumb hitThumb = sender as Thumb;

            if (adornedElement == null || hitThumb == null) return;

            // Ensure that the Width and Height are properly initialized after the resize. 
            EnforceSize(adornedElement);

            // Change the size by the amount the user drags the mouse, as long as it's larger  
            // than the width or height of an adorner, respectively. 
            adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                adornedElement.Height = adornedElement.Width * aspect;
            }
            else
            {
                adornedElement.Height = Math.Max(args.VerticalChange / 4 + adornedElement.Height, hitThumb.DesiredSize.Height);
            }
        }

        // Arrange the Adorners. 
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.   
            // These will be used to place the ResizingAdorner at the corners of the adorned element.   
            double desiredWidth = AdornedElement.DesiredSize.Width;
            double desiredHeight = AdornedElement.DesiredSize.Height;
            // adornerWidth & adornerHeight are used for placement as well. 
            double adornerWidth = this.DesiredSize.Width;
            double adornerHeight = this.DesiredSize.Height;

            topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
            topRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
            bottomLeft.Arrange(new Rect(-adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));
            bottomRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

            // Return the final size. 
            return finalSize;
        }

        // Helper method to instantiate the corner Thumbs, set the Cursor property,  
        // set some appearance properties, and add the elements to the visual tree. 
        void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        {
            if (cornerThumb != null) return;

            cornerThumb = new Thumb();

            // Set some arbitrary visual characteristics. 
            cornerThumb.Cursor = customizedCursor;
            cornerThumb.Height = cornerThumb.Width = 10;
            cornerThumb.Opacity = 0.0;
            cornerThumb.Background = new SolidColorBrush(Colors.MediumBlue);

            visualChildren.Add(cornerThumb);
        }

        // This method ensures that the Widths and Heights are initialized.  Sizing to content produces 
        // Width and Height values of Double.NaN.  Because this Adorner explicitly resizes, the Width and Height 
        // need to be set first.  It also sets the maximum size of the adorned element. 
        void EnforceSize(FrameworkElement adornedElement)
        {
            if (adornedElement.Width.Equals(Double.NaN))
                adornedElement.Width = adornedElement.DesiredSize.Width;
            if (adornedElement.Height.Equals(Double.NaN))
                adornedElement.Height = adornedElement.DesiredSize.Height;

            FrameworkElement parent = adornedElement.Parent as FrameworkElement;
            if (parent != null)
            {
                adornedElement.MaxHeight = parent.ActualHeight;
                adornedElement.MaxWidth = parent.ActualWidth;
            }
        }
        // Override the VisualChildrenCount and GetVisualChild properties to interface with  
        // the adorner's visual collection. 
        protected override int VisualChildrenCount { get { return visualChildren.Count; } }
        protected override Visual GetVisualChild(int index) { return visualChildren[index]; }
    }

}

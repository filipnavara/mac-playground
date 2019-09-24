#if MACOS_THEME

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#elif XAMARINMAC
using AppKit;
using Foundation;
#endif

namespace System.Windows.Forms
{
	[ComVisible(true)]
	[DefaultBindingProperty("Text")]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[DefaultEvent("TextChanged")]
	[Designer("System.Windows.Forms.Design.TextBoxBaseDesigner, " + Consts.AssemblySystem_Design)]
	public abstract partial class TextBoxBase : Control, IMacNativeControl
	{
		#region Local Variables

		internal ITextBoxBaseImp imp;

		internal bool modified = false;
		internal bool multiline = false;
		internal bool read_only = false;
		internal char password_char = '\0';
		internal bool use_system_password_char = false;
		internal bool richtext = false;
		internal bool auto_size = true;
		internal bool backcolor_set = false;
		internal bool forecolor_set = false;
		internal bool accepts_tab = false;
		internal bool accepts_return = false;
		internal int max_length = -1;
		internal HorizontalAlignment alignment = HorizontalAlignment.Left;

		internal BorderStyle actual_border_style = BorderStyle.Fixed3D;
		internal RichTextBoxScrollBars scrollbars = RichTextBoxScrollBars.None;

		internal AutoCompleteStringCollection auto_complete_custom_source;
		internal AutoCompleteMode auto_complete_mode = AutoCompleteMode.None;
		internal AutoCompleteSource auto_complete_source = AutoCompleteSource.None;

		// Just to make friends happy
		internal bool has_been_focused = false;
		internal bool show_caret_w_selection = false;
		internal Document document;
		internal ArrayList list_links;
		internal ImplicitHScrollBar hscroll;
		internal ImplicitVScrollBar vscroll;

		internal delegate string PreprocessTextDelegate(string text, string prev);
		internal PreprocessTextDelegate preprocessText = null;

		#endregion // Local Variables

		internal TextBoxBase()
		{
			imp = new TextBoxBase_Dummy();

			InternalBorderStyle = BorderStyle.Fixed3D;
			FontChanged += HandleFontChanged;
			EnabledChanged += HandleEnabled;
			MultilineChanged += HandleMultilineChanged;

			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		}

		#region IMacNativeControl

		public NSView CreateView()
		{
			if (imp is TextBoxBase_Dummy)
			{
				var newImp = CreateImp();
				var view = newImp.CreateView();
				imp = newImp;
				return view;
			}
			
			return imp.CreateView();
		}

		#endregion //IMacNativeControl

		#region Private and Internal Methods

		internal override Size GetPreferredSizeCore(Size proposedSize)
		{
			Size bordersAndPadding = SizeFromClientSize(Size.Empty) + Padding.Size;
			if (BorderStyle != BorderStyle.None)
				bordersAndPadding += new Size(0, 5);
			proposedSize -= bordersAndPadding;

			TextFormatFlags format = TextFormatFlags.NoPrefix;
			if (!Multiline)
				format |= TextFormatFlags.SingleLine;
			else if (WordWrap)
				format |= TextFormatFlags.WordBreak;

			Size textSize = TextRenderer.MeasureText(this.Text, this.Font, proposedSize, format);
			textSize.Height = Math.Max(textSize.Height, FontHeight);
			return textSize + bordersAndPadding;
		}

		protected virtual void HandleFontChanged(object sender, EventArgs e)
		{
			Imp.ApplyFont(Font);
		}

		private void HandleEnabled(object sender, EventArgs e)
		{
			Imp.ApplyEnabled(Enabled);
		}

		private void HandleMultilineChanged(object sender, EventArgs e)
		{
			RecreateImpIfNeeded();
		}

		internal virtual void HandleLinkClicked(NSTextView textView, NSObject link, nuint charIndex)
		{
		}

		// macOS-specific, but intentionally protected
		protected virtual string PreprocessText(string value)
		{
			value = preprocessText != null ? preprocessText(value, Text) : value;

			value = value ?? String.Empty;
			if (MaxLength >= 0 && value.Length > MaxLength)
				value = value.Substring(0, MaxLength);
			
			return value;
		}

		#endregion // Private and Internal Methods

		#region Public Instance Properties
		[DefaultValue(false)]
		[MWFCategory("Behavior")]
		public bool AcceptsTab
		{
			get { return accepts_tab; }

			set
			{
				if (value != accepts_tab)
				{
					accepts_tab = value;
					OnAcceptsTabChanged(EventArgs.Empty);
				}
			}
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DefaultValue(true)]
		[Localizable(true)]
		[RefreshProperties(RefreshProperties.Repaint)]
		[MWFCategory("Behavior")]
		public override bool AutoSize
		{
			get
			{
				return auto_size;
			}

			set
			{
				if (value != auto_size)
				{
					auto_size = value;
					if (auto_size)
					{
						if (PreferredHeight != Height)
						{
							Height = PreferredHeight;
						}
					}
				}
			}
		}

		[DispId(-501)]
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				backcolor_set = true;
				var c = ChangeBackColor(value);
				base.BackColor = c;
				Imp.ApplyBackColor(c);
			}
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override Image BackgroundImage
		{
			get { return base.BackgroundImage; }
			set { base.BackgroundImage = value; }
		}

		[DefaultValue(BorderStyle.Fixed3D)]
		[DispId(-504)]
		[MWFCategory("Appearance")]
		public BorderStyle BorderStyle
		{
			get { return actual_border_style; }
			set
			{
				if (value == actual_border_style)
					return;

				actual_border_style = value;
				Imp.ApplyBorderStyle(value);

				if (value != BorderStyle.Fixed3D)
					value = BorderStyle.None;

				InternalBorderStyle = value;
				OnBorderStyleChanged(EventArgs.Empty);
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanUndo
		{
			get { return Imp.CanUndo(); }
		}

		[DispId(-513)]
		public override Color ForeColor
		{
			get {
				return base.ForeColor;
			}
			set {
				forecolor_set = true;
				base.ForeColor = value;
				Imp.ApplyForeColor(value);
			}
		}

		[DefaultValue(true)]
		[MWFCategory("Behavior")]
		public bool HideSelection
		{
			get { NotImplemented(MethodBase.GetCurrentMethod()); return false; }
			set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
		}

		[MergableProperty(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Editor("System.Windows.Forms.Design.StringArrayEditor, " + Consts.AssemblySystem_Design, typeof(System.Drawing.Design.UITypeEditor))]
		[Localizable(true)]
		[MWFCategory("Appearance")]
		public string[] Lines
		{
			get
			{
				var text = Text;
				var list = new ArrayList();

				int lineStart = 0;
				while (lineStart < text.Length)
				{
					int lineEnd = lineStart;
					for (; lineEnd < text.Length; lineEnd++)
					{
						char c = text[lineEnd];
						if (c == '\r' || c == '\n')
							break;
					}

					string line = text.Substring(lineStart, lineEnd - lineStart);
					list.Add(line);

					// Treat "\r", "\r\n", and "\n" as new lines
					if (lineEnd < text.Length && text[lineEnd] == '\r')
						lineEnd++;
					if (lineEnd < text.Length && text[lineEnd] == '\n')
						lineEnd++;

					lineStart = lineEnd;
				}

				// Corner case -- last character in Text is a new line; need to add blank line to list
				if (text.Length > 0 && (text[text.Length - 1] == '\r' || text[text.Length - 1] == '\n'))
					list.Add("");

				return (string[])list.ToArray(typeof(string));
			}

			set
			{
				if (value != null && value.Length > 0)
				{
					var text = new StringBuilder(value[0]);
					for (int i = 1; i < value.Length; ++i)
					{
						//text.Append("\r\n");
						text.Append(Environment.NewLine);
						text.Append(value[i]);
					}
					Text = text.ToString();
				}
				else
				{
					Text = "";
				}
			}
		}

		[DefaultValue(32767)]
		[Localizable(true)]
		[MWFCategory("Behavior")]
		public virtual int MaxLength
		{
			get { return max_length; }
			set
			{
				value = value < 0 ? -1 : value;
				if (value != max_length)
				{
					max_length = value;
					Text = PreprocessText(Text);
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Modified
		{
			get
			{
				return modified;
			}

			set
			{
				if (value != modified)
				{
					modified = value;
					OnModifiedChanged(EventArgs.Empty);
				}
			}
		}

		[DefaultValue(false)]
		[Localizable(true)]
		[RefreshProperties(RefreshProperties.All)]
		[MWFCategory("Behavior")]
		public virtual bool Multiline
		{
			get { return multiline; }

			set
			{
				if (value != multiline)
				{
					multiline = value;

					if (!richtext)
						SetStyle(ControlStyles.FixedHeight, !value);

					// SetBoundsCore overrides the Height for multiline if it needs to,
					// so we don't need to worry about it here.
					SetBoundsCore(Left, Top, Width, ExplicitBounds.Height, BoundsSpecified.None);

					if (Parent != null)
						Parent.PerformLayout();

					OnMultilineChanged(EventArgs.Empty);
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		// This returns the preferred outer height, not the client height.
		public int PreferredHeight
		{
			get
			{
				return Imp.IntrinsicContentSize.Height;
			}
		}

		[RefreshProperties(RefreshProperties.Repaint)]
		[DefaultValue(false)]
		[MWFCategory("Behavior")]
		public bool ReadOnly
		{
			get
			{
				return read_only;
			}

			set
			{
				if (value != read_only)
				{
					read_only = value;
					Imp.ApplyReadOnly(read_only);
					OnReadOnlyChanged(EventArgs.Empty);
					Invalidate();
				}
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual string SelectedText
		{
			get { return Imp.SelectedText; }
			set { Imp.SelectedText = value; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int SelectionLength
		{
			get { return Imp.SelectionLength; }
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				// If SelectionStart has been used, we don't highlight on focus
				has_been_focused = true;

				int start = SelectionStart;
				int length = Math.Min(Text.Length - start, value);
				Imp.Select(start, length);
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart
		{
			get { return Imp.SelectionStart; }
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				int start = Math.Min(Text.Length, value);
				int length = Math.Min(Text.Length - start, SelectionLength);
				Imp.Select(start, length);
			}
		}

		[DefaultValue(true)]
		public virtual bool ShortcutsEnabled
		{
			get { NotImplemented(MethodBase.GetCurrentMethod()); return false; }
			set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
		}

		[Editor("System.ComponentModel.Design.MultilineStringEditor, " + Consts.AssemblySystem_Design, "System.Drawing.Design.UITypeEditor, " + Consts.AssemblySystem_Drawing)]
		[Localizable(true)]
		public override string Text
		{
			get { return Imp.Text; }
			set
			{
				// reset to force a select all next time the box gets focus
				has_been_focused = false;

				if (Imp.Text != value)
				{
					Imp.Text = value;
					OnTextChanged(EventArgs.Empty);
				}
			}
		}

		[Browsable(false)]
		public virtual int TextLength
		{
			get { return Imp.Text?.Length ?? 0; }
		}

		[DefaultValue(true)]
		[Localizable(true)]
		[MWFCategory("Behavior")]
		public bool WordWrap
		{
			get { NotImplemented(MethodBase.GetCurrentMethod()); return false; }
			set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override ImageLayout BackgroundImageLayout
		{
			get { return base.BackgroundImageLayout; }
			set { base.BackgroundImageLayout = value; }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Padding Padding
		{
			get { return base.Padding; }
			set { base.Padding = value; }
		}

		protected override Cursor DefaultCursor
		{
			get { return Cursors.IBeam; }
		}
		#endregion // Public Instance Properties

		#region Protected Instance Properties

		protected override bool CanEnableIme
		{
			get { return !(ReadOnly /*|| password_char != '\0'*/); }
		}

		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				cp.ClassName = "EDIT";
				return cp;
			}
		}

		protected override Size DefaultSize
		{
			get { return new Size(100, 20); }
		}

		// Currently our double buffering breaks our scrolling, so don't let people enable this
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool DoubleBuffered
		{
			get { return false; }
			set { }
		}

		#endregion // Protected Instance Properties

		#region Public Instance Methods

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				imp.Release();
			
			base.Dispose(disposing);
		}

		public void AppendText(string text)
		{
			if (String.IsNullOrEmpty(text))
				return;

			Imp.AppendText(text);

			//MoveCaret(CaretDirection.CtrlEnd);
			//SetSelectionToCaret(true);
			//ScrollToCaret();

			// Avoid the initial focus selecting all when append text is used
			has_been_focused = true;

			Modified = false;
			OnTextChanged(EventArgs.Empty);
		}

		public void Clear()
		{
			Modified = false;
			Text = string.Empty;
		}

		public void ClearUndo()
		{
			NotImplemented(MethodBase.GetCurrentMethod());
		}

		public void Copy()
		{
			Imp.Copy();
		}

		public void Cut()
		{
			Imp.Cut();
		}

		public void Paste()
		{
			Imp.Paste();
		}

		public void ScrollToCaret()
		{
			NotImplemented(MethodBase.GetCurrentMethod());
		}

		public void Select(int start, int length)
		{
			Imp.Select(start, length);
		}

		public void SelectAll()
		{
			Imp.SelectAll();
		}

		internal void SelectAllNoScroll()
		{
			Imp.SelectAllNoScroll();
		}

		public override string ToString()
		{
			return String.Concat(base.ToString(), ", Text: ", Text);
		}

		[MonoInternalNote("Deleting is classed as Typing, instead of its own Undo event")]
		public void Undo()
		{
			// TODO
			if (Imp.Undo())
			{
				Modified = true;
				OnTextChanged(EventArgs.Empty);
			}
		}

		public void Redo()
		{
			if (Imp.Redo())
			{
				Modified = true;
				OnTextChanged(EventArgs.Empty);
			}
		}

		public void DeselectAll()
		{
			SelectionLength = 0;
		}

		public virtual char GetCharFromPosition(Point pt)
		{
			return GetCharFromPositionInternal(pt);
		}

		internal virtual char GetCharFromPositionInternal(Point p)
		{
			NotImplemented(MethodBase.GetCurrentMethod(), p);

			// This really shouldn't happen
			return (char)0;
		}

		public virtual int GetCharIndexFromPosition(Point pt)
		{
			NotImplemented(MethodBase.GetCurrentMethod(), pt);
			return 0;
		}

		public virtual Point GetPositionFromCharIndex(int index)
		{
			NotImplemented(MethodBase.GetCurrentMethod(), index);
			return Point.Empty;
		}

		public int GetFirstCharIndexFromLine(int lineNumber)
		{
			NotImplemented(MethodBase.GetCurrentMethod(), lineNumber);
			return -1;
		}

		public int GetFirstCharIndexOfCurrentLine()
		{
			NotImplemented(MethodBase.GetCurrentMethod());
			return 0;
		}

		public virtual int GetLineFromCharIndex(int index)
		{
			NotImplemented(MethodBase.GetCurrentMethod(), index);
			return 0;
		}

		#endregion // Public Instance Methods

		#region Protected Instance Methods

		protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		{
			if (!richtext && !Multiline && AutoSize)
				height = PreferredHeight;

			base.SetBoundsCore(x, y, width, height, specified);
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if ((keyData & Keys.Alt) != 0)
				return base.IsInputKey(keyData);

			switch (keyData & Keys.KeyCode)
			{
				case Keys.Enter:
					return (accepts_return && Multiline);

				case Keys.Tab:
					if ((keyData & Keys.Shift) != 0)
						return false;
					if (AcceptsTab && Multiline)
						if ((keyData & Keys.Control) == 0)
							return true;
					return false;

				case Keys.Home:
				case Keys.End:
				case Keys.Left:
				case Keys.Right:
				case Keys.Up:
				case Keys.Down:
					return Enabled;
				case Keys.PageUp:
				case Keys.PageDown:
					return Enabled && Multiline;
			}
			return false;
		}

		protected virtual void OnAcceptsTabChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[AcceptsTabChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnBorderStyleChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[BorderStyleChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnHideSelectionChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[HideSelectionChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnModifiedChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[ModifiedChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnMultilineChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[MultilineChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected override void OnPaddingChanged(EventArgs e)
		{
			base.OnPaddingChanged(e);
		}

		protected virtual void OnReadOnlyChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[ReadOnlyChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			return base.ProcessCmdKey(ref msg, keyData);
		}
		protected override bool ProcessDialogKey(Keys keyData)
		{
			// The user can use Ctrl-Tab or Ctrl-Shift-Tab to move control focus
			// instead of inserting a Tab.  However, the focus-moving-tab-stuffs
			// doesn't work if Ctrl is pushed, so we remove it before sending it.
			//if (accepts_tab && (keyData & (Keys.Control | Keys.Tab)) == (Keys.Control | Keys.Tab))
			//keyData ^= Keys.Control;

			return base.ProcessDialogKey(keyData);
		}

		internal virtual void RaiseSelectionChanged()
		{
			// Do nothing, overridden in RTB
		}


		protected override void WndProc(ref Message m)
		{
			switch ((Msg)m.Msg)
			{
				case Msg.WM_KEYDOWN:
					WmKeyDown(ref m);
					break;
				default:
					base.WndProc(ref m);
					return;
			}
		}

		private void WmKeyDown(ref Message m)
		{
			Keys keyCode = (Keys)(m.WParam.ToInt32() & (int)Keys.KeyCode);
			if (keyCode == Keys.Return && !accepts_return) // && Multiline && accepts_return)
			{
				base.WndProc(ref m);

				m.Result = (IntPtr)1; // Do not deliver the original event to the native field editor.
				return;
			}

			base.WndProc(ref m);
		}

		#endregion // Protected Instance Methods

		#region Events
		static object AcceptsTabChangedEvent = new object();
		static object AutoSizeChangedEvent = new object();
		static object BorderStyleChangedEvent = new object();
		static object HideSelectionChangedEvent = new object();
		static object ModifiedChangedEvent = new object();
		static object MultilineChangedEvent = new object();
		static object ReadOnlyChangedEvent = new object();
		static object HScrolledEvent = new object();
		static object VScrolledEvent = new object();

		public event EventHandler AcceptsTabChanged
		{
			add { Events.AddHandler(AcceptsTabChangedEvent, value); }
			remove { Events.RemoveHandler(AcceptsTabChangedEvent, value); }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event EventHandler AutoSizeChanged
		{
			add { Events.AddHandler(AutoSizeChangedEvent, value); }
			remove { Events.RemoveHandler(AutoSizeChangedEvent, value); }
		}

		public event EventHandler BorderStyleChanged
		{
			add { Events.AddHandler(BorderStyleChangedEvent, value); }
			remove { Events.RemoveHandler(BorderStyleChangedEvent, value); }
		}

		public event EventHandler HideSelectionChanged
		{
			add { Events.AddHandler(HideSelectionChangedEvent, value); }
			remove { Events.RemoveHandler(HideSelectionChangedEvent, value); }
		}

		public event EventHandler ModifiedChanged
		{
			add { Events.AddHandler(ModifiedChangedEvent, value); }
			remove { Events.RemoveHandler(ModifiedChangedEvent, value); }
		}

		public event EventHandler MultilineChanged
		{
			add { Events.AddHandler(MultilineChangedEvent, value); }
			remove { Events.RemoveHandler(MultilineChangedEvent, value); }
		}

		public event EventHandler ReadOnlyChanged
		{
			add { Events.AddHandler(ReadOnlyChangedEvent, value); }
			remove { Events.RemoveHandler(ReadOnlyChangedEvent, value); }
		}

		internal event EventHandler HScrolled
		{
			add { Events.AddHandler(HScrolledEvent, value); }
			remove { Events.RemoveHandler(HScrolledEvent, value); }
		}

		internal event EventHandler VScrolled
		{
			add { Events.AddHandler(VScrolledEvent, value); }
			remove { Events.RemoveHandler(VScrolledEvent, value); }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event EventHandler BackgroundImageChanged
		{
			add { base.BackgroundImageChanged += value; }
			remove { base.BackgroundImageChanged -= value; }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event EventHandler BackgroundImageLayoutChanged
		{
			add { base.BackgroundImageLayoutChanged += value; }
			remove { base.BackgroundImageLayoutChanged -= value; }
		}

		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public new event MouseEventHandler MouseClick
		{
			add { base.MouseClick += value; }
			remove { base.MouseClick -= value; }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new event EventHandler PaddingChanged
		{
			add { base.PaddingChanged += value; }
			remove { base.PaddingChanged -= value; }
		}

		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public new event EventHandler Click
		{
			add { base.Click += value; }
			remove { base.Click -= value; }
		}

		// XXX should this not manipulate base.Paint?
#pragma warning disable 0067
		[MonoTODO]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event PaintEventHandler Paint;
#pragma warning restore 0067

		#endregion // Events

		#region Private Methods

		internal override bool ScaleChildrenInternal
		{
			get { return false; }
		}

		internal bool ShowSelection
		{
			get { NotImplemented(MethodBase.GetCurrentMethod()); return true; }
			set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
		}

		internal int TopMargin
		{
			get { NotImplemented(MethodBase.GetCurrentMethod()); return 0; }
			set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
		}

		#region UIA Framework Properties

		internal ScrollBar UIAHScrollBar
		{
			get { return null; }
		}

		internal ScrollBar UIAVScrollBar
		{
			get { return null; }
		}

		#endregion UIA Framework Properties

		#endregion // Private Methods

		// This is called just before OnTextChanged is called.
		internal virtual void OnTextUpdate()
		{
		}

		internal virtual void WmCopy(ref Message m)
		{
			DataObject o = new DataObject(DataFormats.Text, SelectedText);
			if (this is RichTextBox)
				o.SetData(DataFormats.Rtf, ((RichTextBox)this).SelectedRtf);
			Clipboard.SetDataObject(o);
			m.Result = IntPtr.Zero;
		}

		internal virtual Color ChangeBackColor(Color backColor)
		{
			return backColor;
		}

		// Hust to make derived classes happy
		internal Graphics CreateGraphicsInternal()
		{
			if (IsHandleCreated)
				return base.CreateGraphics();

			return DeviceContext;
		}

		internal void CalculateDocument()
		{
		}

		internal void CalculateScrollBars()
		{
		}

		internal void InvalidateLinks(Rectangle clip)
		{
		}

		internal virtual void OnPasswordCharChanged(char value)
		{
			RecreateImpIfNeeded();
		}

		internal virtual void OnUseSystemPasswordCharChanged(bool value)
		{
			RecreateImpIfNeeded();
		}

		internal ITextBoxBaseImp Imp
		{
			get { return imp ?? (imp = CreateImp()); }
		}

		internal ITextBoxBaseImp CreateImp()
		{
			if (IsPasswordModeRequired())
				return new TextBoxBase_NSSecureTextField(this);

			if (!multiline && !richtext)
				return new TextBoxBase_NSTextField(this);

			return new TextBoxBase_NSTextView(this);
		}

		internal bool IsPasswordModeRequired()
		{
			return (this is MaskedTextBox)
				|| (this is TextBox textBox) 
					&& (textBox.PasswordChar != '\0' || textBox.UseSystemPasswordChar);
		}

		void RecreateImpIfNeeded()
		{
			if (imp == null)
				return;

			if (ImpSupportsPassword() != IsPasswordModeRequired() 
			    || (multiline != ImpSupportsMultiline())
			    || (richtext && !ImpSupportsMultiline()))
				RecreateImp();
		}

		void RecreateImp()
		{
			if (IsHandleCreated)
				RecreateHandle();
			else
				imp = CreateImp();
		}

		internal virtual bool ImpSupportsPassword()
		{
			return imp is TextBoxBase_NSSecureTextField;
		}

		internal virtual bool ImpSupportsMultiline()
		{
			return imp is TextBoxBase_NSTextView;
		}

		// Just to make friends happy
		internal class LinkRectangle
		{
			public Rectangle LinkAreaRectangle;
			public LineTag LinkTag;

			public LinkRectangle(Rectangle rect)
			{
			}

			public void Scroll(int x_change, int y_change)
			{
			}
		}
	}

	internal interface ITextBoxBaseImp
	{
		NSView CreateView();
		void Release();

		string Rtf { get; set; }
		string Text { get; set; }
		Size IntrinsicContentSize { get; }

		void AppendText(string text);
		void ApplyFont(Font font);
		void ApplyScrollbars(RichTextBoxScrollBars scrollbars);
		void ApplyReadOnly(bool value);
		void ApplyEnabled(bool value);
		void ApplyForeColor(Color value, bool apply = true);
		void ApplyBackColor(Color value, bool apply = true);
		void ApplyBorderStyle(BorderStyle value);
		void ApplyAlignment(HorizontalAlignment value);
	
		void SelectAll();
		void SelectAllNoScroll();
		void Copy();
		void Cut();
		void Paste();
		bool CanUndo();
		bool CanRedo();
		bool Undo();
		bool Redo();

		string SelectedText { get; set; }
		int SelectionStart { get; }
		int SelectionLength { get; }
		void Select(int start, int length);
		Color SelectionColor { get; set; }
	}

	internal class TextBoxBase_Dummy : ITextBoxBaseImp
	{
		public NSView CreateView() { return new NSView(); }
		public void Release() {}

		public string Rtf { get; set; } = String.Empty;
		public string Text { get; set; } = String.Empty;
		public Size IntrinsicContentSize { get { return new Size(100, 20); } }

		public void AppendText(string text) { if (text != null) Text = (Text??String.Empty) + text; }
		public void ApplyFont(Font font) { }
		public void ApplyScrollbars(RichTextBoxScrollBars scrollbars) { }
		public void ApplyReadOnly(bool value) { }
		public void ApplyEnabled(bool value) { }
		public void ApplyForeColor(Color value, bool apply) {}
		public void ApplyBackColor(Color value, bool apply) {}
		public void ApplyBorderStyle(BorderStyle value) {}
		public void ApplyAlignment(HorizontalAlignment value) {}

		public void SelectAll() { }
		public void SelectAllNoScroll() { }
		public void Copy() { }
		public void Cut() { }
		public void Paste() { }
		public bool CanUndo() { return false; }
		public bool CanRedo() { return false; }
		public bool Undo() { return false; }
		public bool Redo() { return false; }

		public string SelectedText { get; set; }
		public int SelectionStart { get; }
		public int SelectionLength { get; }
		public void Select(int start, int length) { }
		public Color SelectionColor { get; set; }
	}
}

#endif //MACOS_THEME
#if MACOS_THEME
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Mac;
using System.Linq;
using System.Reflection;
using System.Windows.Forms.Mac;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using nint = System.Int32;
#elif XAMARINMAC
using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
#endif

namespace System.Windows.Forms
{
	public abstract partial class TextBoxBase
	{
		internal class TextBoxBase_NSTextField : ITextBoxBaseImp
		{
			protected TextBoxBase owner;
			protected NSTextField textField;
			protected string text;
			protected Formatter formatter;
			internal static Class fieldClass;

			public TextBoxBase_NSTextField(TextBoxBase owner)
			{
				this.owner = owner;
			}

			public virtual NSView CreateView()
			{
				text = owner.Text;
				var size = owner.Bounds.Size;

				using (var restore = new NSControlSetCellClass(typeof(NSTextField), typeof(TextFieldCell)))
					textField = new TextField(new CGRect(0, 0, size.Width, size.Height));

				textField.TextShouldBeginEditing = TextFieldShouldBeginEditing;
				textField.Changed += TextFieldChanged;
				textField.DoCommandBySelector = DoCommandBySelector;
				textField.GetCompletions = TextFieldGetCompletions;
				textField.Formatter = formatter = new Formatter(this);
				textField.UsesSingleLineMode = true;
				if (textField.Cell is NSTextFieldCell cell)
				{
					// This is what makes the field really "single-line" - with horizontal scrolling.
					cell.Wraps = false;
					cell.Scrollable = true;
				}

				ApplyBorderStyle(owner.BorderStyle);
				ApplyForeColor(owner.ForeColor, owner.forecolor_set);
				ApplyBackColor(owner.BackColor, owner.backcolor_set);
				ApplyAlignment(owner.alignment);
				ApplyFont(owner.Font);
				ApplyScrollbars(owner.scrollbars);
				ApplyReadOnly(owner.read_only);
				ApplyEnabled(owner.is_enabled);
				ApplyText(text);

				return textField;
			}

			public virtual void Release()
			{
				if (textField != null)
				{
					textField.AbortEditing();
					textField.TextShouldBeginEditing = null;
					textField.Changed -= TextFieldChanged;
					textField.DoCommandBySelector = null;
					textField.GetCompletions = null;
					textField = null;

					if (formatter != null)
					{
						formatter.owner = null;
						formatter = null;
					}

					owner = null;
				}
			}

			public virtual string Rtf { get { return null; } set { } }

			public virtual string Text
			{
				get { return text ?? String.Empty; }
				set
				{
					this.text = value;
					if (textField != null)
						ApplyText(value);
				}
			}

			public virtual void ApplyReadOnly(bool value)
			{
				// Now handled by TextFieldShouldBeginEditing(), which let's the user to select, copy and lookup content.

				//if (textField != null)
					//textField.Editable = !value;
			}

			public virtual void ApplyEnabled(bool value)
			{
				if (textField != null)
					textField.Enabled = value;
			}

			public virtual Size IntrinsicContentSize
			{
				get { return textField?.IntrinsicContentSize.ToSDSize() ?? new Size(100, 20); }
			}

			public virtual void AppendText(string value)
			{
				if (textField == null)
					text = text == null ? value : text + value ?? String.Empty;
				else
					textField.StringValue = textField.StringValue == null ? value : textField.StringValue + value ?? String.Empty;
			}

			public virtual void ApplyFont(Font font)
			{
				if (textField != null)
					textField.Font = font.ToNSFont();
			}

			public virtual void ApplyBorderStyle(BorderStyle value)
			{
				if (textField == null)
					return;

				switch (value)
				{
					case BorderStyle.None:
						textField.Bezeled = false;
						break;
					case BorderStyle.FixedSingle:
					case BorderStyle.Fixed3D:
						textField.BezelStyle = NSTextFieldBezelStyle.Square;
						textField.Bezeled = true;
						break;
				}
			}

			public virtual void ApplyForeColor(Color value, bool apply)
			{
				if (textField != null && apply)
					textField.TextColor = value.ToNSColor();
			}

			public virtual void ApplyBackColor(Color value, bool apply)
			{
				if (textField != null && apply)
					textField.BackgroundColor = value.ToNSColor();
			}

			public virtual void ApplyAlignment(HorizontalAlignment value)
			{
				if (textField != null)
					textField.Alignment = value.ToNSTextAlignment();
			}

			public virtual void ApplyScrollbars(RichTextBoxScrollBars scrollbars)
			{
				// No scrollbars in this implementation
			}

			public virtual string SelectedText
			{
				get
				{
					return Text.Substring(SelectionStart, SelectionLength);
				}
				set
				{
					int selectionStart = SelectionStart;
					Text = Text.Substring(0, selectionStart) + value + Text.Substring(selectionStart + SelectionLength);
					Select(selectionStart + value.Length, 0);
				}
			}

			public Color SelectionColor
			{
				// SelectionColor is currently only implemented for TextBoxBase_NSTextView which is used
				// for multi-line and RTF input
				get { 
					NotImplemented(MethodBase.GetCurrentMethod());
					return textField != null ? textField.TextColor.ToSDColor() : owner.ForeColor;
				}
				set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
			}

			public virtual int SelectionStart
			{
				get
				{
					if (textField != null && textField.CurrentEditor != null)
						return (int)textField.CurrentEditor.SelectedRange.Location;

					return 0;
				}
			}

			public virtual int SelectionLength
			{
				get
				{
					if (textField != null && textField.CurrentEditor != null)
						return (int)textField.CurrentEditor.SelectedRange.Length;

					return 0;
				}
			}

			public virtual void Select(int start, int length)
			{
				if (textField != null && textField.CurrentEditor != null)
				{
					NSRange range;
					range.Location = start;
					range.Length = length;
					textField.CurrentEditor.SelectedRange = range;
				}
				else
				{
					if (textField is TextField tfield)
					{
						tfield.preSelectionStart = start;
						tfield.preSelectionLength = length;
					}
				}
			}

			public virtual void SelectAll()
			{
				if (textField != null)
					textField.SelectText(textField);
			}

			public virtual void SelectAllNoScroll()
			{
				// FIXME
				if (textField != null)
					textField.SelectText(textField);
			}

			public virtual void Copy()
			{
				if (!(textField is NSSecureTextField))
					SendActionToTextView("copy:");
			}

			public virtual void Cut()
			{
				if (!(textField is NSSecureTextField))
					SendActionToTextView("cut:");
			}

			public virtual void Paste()
			{
				SendActionToTextView("paste:");
			}

			public virtual bool CanUndo()
			{
				Control.NotImplemented(MethodBase.GetCurrentMethod(), text);
				return textField != null; // FIXME
			}

			public virtual bool CanRedo()
			{
				Control.NotImplemented(MethodBase.GetCurrentMethod(), text);
				return textField != null; // FIXME
			}

			public virtual bool Undo()
			{
				return SendActionToTextView("undo:");
			}

			public virtual bool Redo()
			{
				return SendActionToTextView("redo:");
			}

			internal virtual void ApplyText(string value)
			{
				if (textField != null && value != null)
					textField.StringValue = value;
			}

			internal virtual void ApplySelection(int preSelectStart, int preSelectLength)
			{
				if (preSelectLength != -1 && preSelectStart != -1)
					Select(preSelectStart, preSelectLength);
			}

			internal virtual void ApplyFormatter(NSFormatter formatter)
			{
				if (textField != null && formatter != null)
					textField.Formatter = formatter;
			}

			bool completing = false;
			protected virtual void TextFieldChanged(object sender, EventArgs e)
			{
				this.text = textField.StringValue;

				owner.OnTextUpdate();
				owner.OnTextChanged(e);

				Complete();
			}

			internal virtual void Complete()
			{
				if (!completing
					&& owner.SelectionStart == owner.TextLength
					&& owner.auto_complete_mode != AutoCompleteMode.None && owner.auto_complete_source == AutoCompleteSource.CustomSource && owner.auto_complete_custom_source != null
					&& textField.StringValue.Length > 0 && owner.auto_complete_custom_source.Count != 0
					&& textField.Window.FirstResponder is NSTextView textView)
				{
						completing = true;
						textView.Complete(textField);
						completing = false;
				}
			}

			internal virtual string FormatterPreprocessText(string value)
			{
				return owner.PreprocessText(value);
			}

			internal virtual bool SendActionToTextView(string action)
			{
				if (textField != null && textField.Window.FirstResponder is NSTextView textView && textField.Contains(textView))
					return NSApplication.SharedApplication.SendAction(new Selector(action), textView, NSApplication.SharedApplication);
				return false;
			}

			#region NSTextFieldDelegate

			internal virtual bool DoCommandBySelector(NSControl control, NSTextView textView, Selector selector)
			{
				switch (selector.Name)
				{
					case "insertTab:":
					case "insertBacktab:":
						SendWmKey(VirtualKeys.VK_TAB, IntPtr.Zero);
						return true;
					case "insertNewline:":
						SendWmKey(VirtualKeys.VK_RETURN, IntPtr.Zero);
						return true;
					case "cancelOperation:":
						SendWmKey(VirtualKeys.VK_ESCAPE, IntPtr.Zero);
						return true;
				}
				return false;
			}

			internal virtual bool TextFieldShouldBeginEditing(NSControl control, NSText fieldEditor)
			{
				return !owner.ReadOnly && owner.Enabled;
			}

			internal string[] TextFieldGetCompletions(NSControl control, NSTextView textView, string[] words, NSRange charRange, ref nint index)
			{
				var prefix = textView.String?.Substring(0, (int)charRange.Location + (int)charRange.Length) ?? String.Empty;

				var completions = new List<string>();
				foreach(string s in owner.auto_complete_custom_source)
					if (s.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
						completions.Add(s.Substring((int)charRange.Location));

				index = -1;
				return completions.Distinct().OrderBy(x => x).ToArray();
			}

			internal virtual void SendWmKey(VirtualKeys key, IntPtr lParam)
			{
				XplatUI.SendMessage(owner.Handle, Msg.WM_KEYDOWN, (IntPtr)key, lParam);

				if (owner != null && owner.IsHandleCreated) // The previous line could have caused disposing the control (esc, enter, ...)
					XplatUI.SendMessage(owner.Handle, Msg.WM_KEYUP, (IntPtr)key, lParam);
			}

			#endregion //NSTextFieldDelegate
		}

		internal class TextFieldCell : NSTextFieldCell
		{
			static NSLayoutManager lm = new NSLayoutManager();

			[Export("init:")]
			public TextFieldCell() : base(String.Empty)
			{
			}

			[Export("initTextCell:")]
			public TextFieldCell(string text) : base(text ?? String.Empty)
			{
			}

			public override CGRect DrawingRectForBounds(CGRect theRect)
			{
				var lineHeight = lm.DefaultLineHeightForFont(Font);

				// Vertical text position compensation for the case when the text field is too narrow.
				// 1px is not enough, but 2px would cause artifacts.
				var dr = base.DrawingRectForBounds(theRect);
				if (lineHeight > dr.Height)
					dr.Y -= 1;

				return dr;
			}
		}

		internal class Formatter : NSFormatter
		{
			internal TextBoxBase_NSTextField owner;

			public Formatter(TextBoxBase_NSTextField owner)
			{
				this.owner = owner;
			}

			public override string StringFor(NSObject value)
			{
				return owner.FormatterPreprocessText(value?.ToString() ?? null);
			}

			public override bool GetObjectValue(out NSObject obj, string str, out NSString error)
			{
				try
				{
					obj = (NSString)owner.FormatterPreprocessText(str ?? String.Empty);
					error = null;
					return true;
				}
				catch (Exception e)
				{
					obj = (NSString)string.Empty;
					error = (NSString)e.Message;	
					return false;
				}
			}
		}
	}

	internal class TextField : NSTextField
	{
		public int preSelectionStart = -1;
		public int preSelectionLength = -1;

		public TextField(CGRect frame) : base (frame)
		{
		}

		public TextField(IntPtr handle) : base(handle)
		{
		}

		public override bool BecomeFirstResponder()
		{
			var result = base.BecomeFirstResponder();

			if (result && preSelectionStart != -1 && preSelectionLength != -1 && CurrentEditor != null)
			{
				CurrentEditor.SelectedRange = new NSRange { Location = preSelectionStart, Length = preSelectionLength };
				preSelectionStart = preSelectionLength = -1;
			}

			return result;
		}
	}
}

#endif //MACOS_THEME

#if MACOS_THEME
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

namespace System.Windows.Forms {

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[Designer("System.Windows.Forms.Design.TextBoxDesigner, " + Consts.AssemblySystem_Design, "System.ComponentModel.Design.IDesigner")]
	public class TextBox : TextBoxBase
	{
		#region Public Constructors

		public TextBox()
		{
			SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
			SetStyle(ControlStyles.FixedHeight, true);
		}

		#endregion // Public Constructors

		#region Public Instance Properties
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[Localizable(true)]
		[Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, " + Consts.AssemblySystem_Design, "System.Drawing.Design.UITypeEditor, " + Consts.AssemblySystem_Drawing)]
		public AutoCompleteStringCollection AutoCompleteCustomSource
		{
			get
			{
				if (auto_complete_custom_source == null)
				{
					auto_complete_custom_source = new AutoCompleteStringCollection();
					auto_complete_custom_source.CollectionChanged += OnAutoCompleteCustomSourceChanged;
				}
				return auto_complete_custom_source;
			}
			set
			{
				if (auto_complete_custom_source == value)
					return;

				if (auto_complete_custom_source != null) //remove eventhandler from old collection
					auto_complete_custom_source.CollectionChanged -= OnAutoCompleteCustomSourceChanged;

				auto_complete_custom_source = value;

				if (auto_complete_custom_source != null)
					auto_complete_custom_source.CollectionChanged += OnAutoCompleteCustomSourceChanged;
			}
		}

		internal virtual void OnAutoCompleteCustomSourceChanged(object sender, CollectionChangeEventArgs e)
		{
			if (auto_complete_source == AutoCompleteSource.CustomSource)
			{
				//FIXME: handle add, remove and refresh events in AutoComplete algorithm.
			}
		}

		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[DefaultValue(AutoCompleteMode.None)]
		public AutoCompleteMode AutoCompleteMode
		{
			get { return auto_complete_mode; }
			set
			{
				if (auto_complete_mode == value)
					return;

				if ((value < AutoCompleteMode.None) || (value > AutoCompleteMode.SuggestAppend))
					throw new InvalidEnumArgumentException(Locale.GetText("Enum argument value '{0}' is not valid for AutoCompleteMode", value));

				auto_complete_mode = value;
			}
		}

		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[DefaultValue(AutoCompleteSource.None)]
		[TypeConverter(typeof(TextBoxAutoCompleteSourceConverter))]
		public AutoCompleteSource AutoCompleteSource
		{
			get { return auto_complete_source; }
			set
			{
				if (auto_complete_source == value)
					return;

				if (!Enum.IsDefined(typeof(AutoCompleteSource), value))
					throw new InvalidEnumArgumentException(Locale.GetText("Enum argument value '{0}' is not valid for AutoCompleteSource", value));

				auto_complete_source = value;
			}
		}

		[DefaultValue(false)]
		[RefreshProperties(RefreshProperties.Repaint)]
		public bool UseSystemPasswordChar
		{
			get { return use_system_password_char; }
			set 
			{
				if (use_system_password_char != value)
				{
					use_system_password_char = value;
					OnUseSystemPasswordCharChanged(value);
				}
			}
		}

		[DefaultValue(false)]
		[MWFCategory("Behavior")]
		public bool AcceptsReturn
		{
			get { return accepts_return; }

			set
			{
				if (value != accepts_return)
					accepts_return = value;
			}
		}

		[DefaultValue(CharacterCasing.Normal)]
		[MWFCategory("Behavior")]
		public CharacterCasing CharacterCasing
		{
			get { NotImplemented(MethodBase.GetCurrentMethod()); return CharacterCasing.Normal; }
			set { NotImplemented(MethodBase.GetCurrentMethod(), value); }
		}

		[Localizable(true)]
		[DefaultValue('\0')]
		[MWFCategory("Behavior")]
		[RefreshProperties(RefreshProperties.Repaint)]
		public char PasswordChar
		{
			get { return password_char; }
			set
			{
				if (password_char != value)
				{
					password_char = value;
					OnPasswordCharChanged(value);
				}
			}
		}

		[DefaultValue(ScrollBars.None)]
		[Localizable(true)]
		[MWFCategory("Appearance")]
		public ScrollBars ScrollBars
		{
			get
			{
				return (ScrollBars)scrollbars;
			}

			set
			{
				if (!Enum.IsDefined(typeof(ScrollBars), value))
					throw new InvalidEnumArgumentException("value", (int)value, typeof(ScrollBars));

				if (value != (ScrollBars)scrollbars)
				{
					scrollbars = (RichTextBoxScrollBars)value;
					base.CalculateScrollBars();
				}
			}
		}

		[DefaultValue(HorizontalAlignment.Left)]
		[Localizable(true)]
		[MWFCategory("Appearance")]
		public HorizontalAlignment TextAlign
		{
			get
			{
				return alignment;
			}

			set
			{
				if (value != alignment)
				{
					alignment = value;
					Imp.ApplyAlignment(value);
					OnTextAlignChanged(EventArgs.Empty);
				}
			}
		}

		#endregion // Public Instance Properties

		public void Paste(string text)
		{
			NotImplemented(MethodBase.GetCurrentMethod(), text);
		}

		#region Protected Instance Methods

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (!has_been_focused)
			{
				has_been_focused = true;

				if (SelectionLength == 0 && Text.Length != 0 && Control.MouseButtons == MouseButtons.None)
					SelectAllNoScroll();
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		protected virtual void OnTextAlignChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[TextAlignChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		#endregion // Protected Instance Methods

		#region Events
		static object TextAlignChangedEvent = new object();

		public event EventHandler TextAlignChanged
		{
			add { Events.AddHandler(TextAlignChangedEvent, value); }
			remove { Events.RemoveHandler(TextAlignChangedEvent, value); }
		}
		#endregion // Events
	}

	internal class TextBoxAutoCompleteSourceConverter : EnumConverter
	{
		public TextBoxAutoCompleteSourceConverter(Type type)
			: base(type)
		{ }

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			StandardValuesCollection stdv = base.GetStandardValues(context);
			AutoCompleteSource[] arr = new AutoCompleteSource[stdv.Count];
			stdv.CopyTo(arr, 0);
			AutoCompleteSource[] arr2 = Array.FindAll(arr, delegate (AutoCompleteSource value) {
				// No "ListItems" in a TextBox.
				return value != AutoCompleteSource.ListItems;
			});
			return new StandardValuesCollection(arr2);
		}
	}
}
#endif
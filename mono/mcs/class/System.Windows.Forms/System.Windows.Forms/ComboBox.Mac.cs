﻿#if MACOS_THEME

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Mac;
using System.Windows.Forms.Mac;
using System.Collections.Generic;
using System.Linq;

#if XAMARINMAC
using AppKit;
using Foundation;
using ObjCRuntime;
#elif MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
#endif

namespace System.Windows.Forms
{
	[DefaultProperty("Items")]
	[DefaultEvent("SelectedIndexChanged")]
	[Designer("System.Windows.Forms.Design.ComboBoxDesigner, " + Consts.AssemblySystem_Design, "System.ComponentModel.Design.IDesigner")]
	[DefaultBindingProperty("Text")]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[ComVisible(true)]
	public class ComboBox : ListControl, IMacNativeControl
	{
		internal ComboBoxStyle dropdown_style = ComboBoxStyle.DropDown;
		internal int selected_index = -1;
		private ObjectCollection items;

		IComboBoxImp imp;

		[ComVisible(true)]
		public class ChildAccessibleObject : AccessibleObject
		{
			public ChildAccessibleObject(ComboBox owner, IntPtr handle) : base(owner)
			{
			}
		}

		public ComboBox()
		{
			items = new ObjectCollection(this);
			imp = new DummyImp(this);
		}

		public NSView CreateView()
		{
			if (imp is DummyImp)
				RecreateImp();

			return imp.Control;
		}

		internal virtual IComboBoxImp CreateImp()
		{
			switch (dropdown_style)
			{
				case ComboBoxStyle.DropDown:
					return new ComboBoxImp(this);
				default:
					return new PopUpImp(this);
			}
		}

		internal virtual void RecreateImp()
		{
			var prev = imp;
			imp = CreateImp();
			prev?.Release();
		}

		internal virtual void OnImpSelectedItemChanged(object sender, EventArgs e)
		{
			selected_index = imp.IndexOfSelectedItem;

			OnSelectedValueChanged(EventArgs.Empty);
			OnSelectedIndexChanged(EventArgs.Empty);
			OnSelectedItemChanged(EventArgs.Empty);
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			imp.Enabled = Enabled;
			base.OnEnabledChanged(e);
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			if (this.AutoSize && imp.Control != null)
				return imp.Control.SizeThatFits(proposedSize.ToCGSize()).ToSDSize();
			return base.GetPreferredSize(proposedSize);
		}

		#region events

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

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event EventHandler DoubleClick
		{
			add { base.DoubleClick += value; }
			remove { base.DoubleClick -= value; }
		}

		static object DrawItemEvent = new object();
		static object DropDownEvent = new object();
		static object DropDownStyleChangedEvent = new object();
		static object MeasureItemEvent = new object();
		static object SelectedIndexChangedEvent = new object();
		static object SelectionChangeCommittedEvent = new object();
		static object DropDownClosedEvent = new object();
		static object TextUpdateEvent = new object();

		public event DrawItemEventHandler DrawItem
		{
			add { Events.AddHandler(DrawItemEvent, value); }
			remove { Events.RemoveHandler(DrawItemEvent, value); }
		}

		public event EventHandler DropDown
		{
			add { Events.AddHandler(DropDownEvent, value); }
			remove { Events.RemoveHandler(DropDownEvent, value); }
		}
		public event EventHandler DropDownClosed
		{
			add { Events.AddHandler(DropDownClosedEvent, value); }
			remove { Events.RemoveHandler(DropDownClosedEvent, value); }
		}

		public event EventHandler DropDownStyleChanged
		{
			add { Events.AddHandler(DropDownStyleChangedEvent, value); }
			remove { Events.RemoveHandler(DropDownStyleChangedEvent, value); }
		}

		public event MeasureItemEventHandler MeasureItem
		{
			add { Events.AddHandler(MeasureItemEvent, value); }
			remove { Events.RemoveHandler(MeasureItemEvent, value); }
		}
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event EventHandler PaddingChanged
		{
			add { base.PaddingChanged += value; }
			remove { base.PaddingChanged -= value; }
		}

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public new event PaintEventHandler Paint
		{
			add { base.Paint += value; }
			remove { base.Paint -= value; }
		}

		public event EventHandler SelectedIndexChanged
		{
			add { Events.AddHandler(SelectedIndexChangedEvent, value); }
			remove { Events.RemoveHandler(SelectedIndexChangedEvent, value); }
		}

		public event EventHandler SelectionChangeCommitted
		{
			add { Events.AddHandler(SelectionChangeCommittedEvent, value); }
			remove { Events.RemoveHandler(SelectionChangeCommittedEvent, value); }
		}
		public event EventHandler TextUpdate
		{
			add { Events.AddHandler(TextUpdateEvent, value); }
			remove { Events.RemoveHandler(TextUpdateEvent, value); }
		}

		#endregion Events

		#region Public Properties
		[MonoTODO("AutoCompletion algorithm is currently not implemented.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[Localizable(true)]
		[Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, " + Consts.AssemblySystem_Design,
			 "System.Drawing.Design.UITypeEditor, " + Consts.AssemblySystem_Drawing)]
		public AutoCompleteStringCollection AutoCompleteCustomSource
		{
			get; set;
		}

		[MonoTODO("AutoCompletion algorithm is currently not implemented.")]
		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[DefaultValue(AutoCompleteMode.None)]
		public AutoCompleteMode AutoCompleteMode
		{
			get; set;
		}

		[MonoTODO("AutoCompletion algorithm is currently not implemented.")]
		[Browsable(true)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[DefaultValue(AutoCompleteSource.None)]
		public AutoCompleteSource AutoCompleteSource
		{
			get; set;
		}

		[DefaultValue((string)null)]
		[AttributeProvider(typeof(IListSource))]
		[RefreshProperties(RefreshProperties.Repaint)]
		[MWFCategory("Data")]
		public new object DataSource
		{
			get { return base.DataSource; }
			set { base.DataSource = value; }
		}

		protected override Size DefaultSize
		{
			get { return new Size(121, 21); }
		}

		[RefreshProperties(RefreshProperties.Repaint)]
		[DefaultValue(DrawMode.Normal)]
		[MWFCategory("Behavior")]
		public DrawMode DrawMode
		{
			get; set;
		}

		[Browsable(true)]
		[DefaultValue(106)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		[MWFCategory("Behavior")]
		public int DropDownHeight
		{
			get; set;
		}

		[DefaultValue(ComboBoxStyle.DropDown)]
		[RefreshProperties(RefreshProperties.Repaint)]
		[MWFCategory("Appearance")]
		public ComboBoxStyle DropDownStyle
		{
			get { return dropdown_style; }
			set
			{
				if (value != dropdown_style)
				{
					dropdown_style = value;

					if (!ImpSupportsStyle(imp, value))
						RecreateImp();
				}
			}
		}

		private bool ImpSupportsStyle(IComboBoxImp popup, ComboBoxStyle value)
		{
			if (imp is DummyImp)
				return true;
			
			switch(value)
			{
				case ComboBoxStyle.Simple:
				case ComboBoxStyle.DropDown:
					return popup is ComboBoxImp;
				case ComboBoxStyle.DropDownList:
					return popup is PopUpImp;
				default:
					return false;
			}
		}

		[MWFCategory("Behavior")]
		public int DropDownWidth
		{
			get; set;
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DroppedDown
		{
			get; set;
		}

		[DefaultValue(FlatStyle.Standard)]
		[Localizable(true)]
		[MWFCategory("Appearance")]
		public FlatStyle FlatStyle
		{
			get; set;
		}

		[DefaultValue(true)]
		[Localizable(true)]
		[MWFCategory("Behavior")]
		public bool IntegralHeight
		{
			get; set;
		}

		[Localizable(true)]
		[MWFCategory("Behavior")]
		public int ItemHeight
		{
			get; set;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[Localizable(true)]
		[Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, " + Consts.AssemblySystem_Design, typeof(System.Drawing.Design.UITypeEditor))]
		[MergableProperty(false)]
		[MWFCategory("Data")]
		public ComboBox.ObjectCollection Items
		{
			get { return items; }
		}

		[DefaultValue(8)]
		[Localizable(true)]
		[MWFCategory("Behavior")]
		public int MaxDropDownItems
		{
			get; set;
		}

		public override Size MaximumSize
		{
			get { return base.MaximumSize; }
			set { base.MaximumSize = new Size(value.Width, 0); }
		}

		[DefaultValue(0)]
		[Localizable(true)]
		[MWFCategory("Behavior")]
		public int MaxLength
		{
			get; set;
		}

		public override Size MinimumSize
		{
			get { return base.MinimumSize; }
			set { base.MinimumSize = new Size(value.Width, 0); }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Browsable(false)]
		public new Padding Padding
		{
			get { return base.Padding; }
			set { base.Padding = value; }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public int PreferredHeight
		{
			get {
				var c = imp?.Control;
				if (c != null)
					return (int)c.FittingSize.Height - (int)c.AlignmentRectInsets.Bottom - (int)c.AlignmentRectInsets.Top;
				return Font.Height + 8;
			}
			//get { return Font.Height + 8; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override int SelectedIndex
		{
			get { return selected_index; }
			set { SetSelectedIndex(value, false); }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Bindable(true)]
		public object SelectedItem
		{
			get { return selected_index == -1 ? null : Items[selected_index]; }
			set
			{
				object item = selected_index == -1 ? null : Items[selected_index];
				if (item == value)
					return;

				if (value == null)
					SelectedIndex = -1;
				else
					SelectedIndex = Items.IndexOf(value);
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string SelectedText
		{
			get
			{
				if (dropdown_style == ComboBoxStyle.DropDownList)
					return string.Empty;

				return imp.SelectedText;
			}
			set
			{
				if (dropdown_style == ComboBoxStyle.DropDownList)
					return;

				imp.SelectedText = value;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionLength
		{
			get; set;
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart
		{
			get; set;
		}

		[DefaultValue(false)]
		[MWFCategory("Behavior")]
		public bool Sorted
		{
			get; set;
		}

		[Bindable(true)]
		[Localizable(true)]
		public override string Text
		{
			get
			{
				return imp?.Text ?? String.Empty;
			}
			set
			{
				if (value == null)
				{
					if (SelectedIndex == -1)
						imp.Text = String.Empty;
					else
						SelectedIndex = -1;
					return;
				}

				// don't set the index if value exactly matches text of selected item
				if (SelectedItem == null || string.Compare(value, GetItemText(SelectedItem), false, CultureInfo.CurrentCulture) != 0)
				{
					// find exact match using case-sensitive comparison, and if does
					// not result in any match then use case-insensitive comparison
					int index = FindStringExact(value, -1, false);
					if (index == -1)
					{
						index = FindStringExact(value, -1, true);
					}
					if (index != -1)
					{
						SelectedIndex = index;
						return;
					}
				}

				imp.Text = value;
			}
		}

		#endregion Public Properties

		#region Public Methods
		[Obsolete("This method has been deprecated")]
		protected virtual void AddItemsCore(object[] value)
		{

		}

		public void BeginUpdate()
		{
		}

		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return base.CreateAccessibilityInstance();
		}

		protected override void CreateHandle()
		{
			base.CreateHandle();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			base.Dispose(disposing);
		}

		public void EndUpdate()
		{
			UpdatedItems();
			Refresh();
		}

		public int FindString(string s)
		{
			return FindString(s, -1);
		}

		public int FindString(string s, int startIndex)
		{
			if (s == null || Items.Count == 0)
				return -1;

			if (startIndex < -1 || startIndex >= Items.Count)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			int i = startIndex;
			if (i == (Items.Count - 1))
				i = -1;
			do
			{
				i++;
				if (string.Compare(s, 0, GetItemText(Items[i]), 0, s.Length, true) == 0)
					return i;
				if (i == (Items.Count - 1))
					i = -1;
			} while (i != startIndex);

			return -1;
		}

		public int FindStringExact(string s)
		{
			return FindStringExact(s, -1);
		}

		public int FindStringExact(string s, int startIndex)
		{
			return FindStringExact(s, startIndex, true);
		}

		private int FindStringExact(string s, int startIndex, bool ignoreCase)
		{
			if (s == null || Items.Count == 0)
				return -1;

			if (startIndex < -1 || startIndex >= Items.Count)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			int i = startIndex;
			if (i == (Items.Count - 1))
				i = -1;
			do
			{
				i++;
				if (string.Compare(s, GetItemText(Items[i]), ignoreCase, CultureInfo.CurrentCulture) == 0)
					return i;
				if (i == (Items.Count - 1))
					i = -1;
			} while (i != startIndex);

			return -1;
		}

		public int GetItemHeight(int index)
		{
			return ItemHeight;
		}

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData & ~Keys.Modifiers)
			{
				case Keys.Up:
				case Keys.Down:
				case Keys.Left:
				case Keys.Right:
				case Keys.PageUp:
				case Keys.PageDown:
				case Keys.Home:
				case Keys.End:
					return true;

				default:
					return false;
			}
		}

		protected override void OnDataSourceChanged(EventArgs e)
		{
			base.OnDataSourceChanged(e);
			BindDataItems();

			/** 
			 ** This 'Debugger.IsAttached' hack is here because of
			 ** Xamarin Bug #2234, which noted that when changing
			 ** the DataSource, in Windows exceptions are eaten
			 ** when SelectedIndexChanged is fired.  However, when
			 ** the debugger is running (i.e. in MonoDevelop), we
			 ** want to be alerted of exceptions.
			 **/

			if (Debugger.IsAttached)
			{
				SetSelectedIndex();
			}
			else
			{
				try
				{
					SetSelectedIndex();
				}
				catch
				{
					//ignore exceptions here per 
					//bug 2234
				}
			}
		}

		private void SetSelectedIndex()
		{
			if (DataSource == null || DataManager == null)
			{
				SelectedIndex = -1;
			}
			else
			{
				SelectedIndex = DataManager.Position;
			}
		}

		protected override void OnDisplayMemberChanged(EventArgs e)
		{
			base.OnDisplayMemberChanged(e);

			if (DataManager == null)
				return;

			SelectedIndex = DataManager.Position;

			//if (selected_index != -1 && DropDownStyle != ComboBoxStyle.DropDownList)
			//SetControlText(GetItemText(Items[selected_index]), true);

			if (!IsHandleCreated)
				return;

			Invalidate();
		}

		protected virtual void OnDrawItem(DrawItemEventArgs e)
		{
			DrawItemEventHandler eh = (DrawItemEventHandler)(Events[DrawItemEvent]);
			if (eh != null)
				eh(this, e);
		}

		internal void HandleDrawItem(DrawItemEventArgs e)
		{
			// Only raise OnDrawItem if we are in an OwnerDraw mode
			switch (DrawMode)
			{
				case DrawMode.OwnerDrawFixed:
				case DrawMode.OwnerDrawVariable:
					OnDrawItem(e);
					break;
				default:
					ThemeEngine.Current.DrawComboBoxItem(this, e);
					break;
			}
		}

		protected virtual void OnDropDown(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[DropDownEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnDropDownClosed(EventArgs e)
		{
			EventHandler eh = (EventHandler)Events[DropDownClosedEvent];
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnDropDownStyleChanged(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[DropDownStyleChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);

			imp.Font = font;

			if (IntegralHeight)
				UpdateComboBoxBounds();

			LayoutComboBox();
		}

		private void UpdateComboBoxBounds()
		{
		}

		protected override void OnForeColorChanged(EventArgs e)
		{
			base.OnForeColorChanged(e);
			//if (textbox_ctrl != null)
			//textbox_ctrl.ForeColor = ForeColor;
		}

        internal override int OverrideHeight(int height)
        {
            return PreferredHeight;
        }

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			//if (textbox_ctrl != null)
			//Controls.AddImplicit(textbox_ctrl);

			LayoutComboBox();
			UpdateComboBoxBounds();
		}

		protected virtual void OnMeasureItem(MeasureItemEventArgs e)
		{
			MeasureItemEventHandler eh = (MeasureItemEventHandler)(Events[MeasureItemEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected override void OnParentBackColorChanged(EventArgs e)
		{
			base.OnParentBackColorChanged(e);
		}

		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			base.OnSelectedIndexChanged(e);

			EventHandler eh = (EventHandler)(Events[SelectedIndexChangedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected virtual void OnSelectedItemChanged(EventArgs e)
		{
		}

		protected virtual void OnSelectionChangeCommitted(EventArgs e)
		{
			EventHandler eh = (EventHandler)(Events[SelectionChangeCommittedEvent]);
			if (eh != null)
				eh(this, e);
		}

		protected override void RefreshItems()
		{
			for (int i = 0; i < Items.Count; i++)
				RefreshItem(i);

			LayoutComboBox();
			Refresh();

			// TODO
			//if (selected_index != -1 && DropDownStyle != ComboBoxStyle.DropDownList)
			//SetControlText(GetItemText(Items[selected_index]), false);
		}

		public override void ResetText()
		{
			Text = String.Empty;
		}

		protected virtual void OnTextUpdate(EventArgs e)
		{
			EventHandler eh = (EventHandler)Events[TextUpdateEvent];
			if (eh != null)
				eh(this, e);
		}

		public void Select(int start, int length)
		{
			if (start < 0)
				throw new ArgumentException("Start cannot be less than zero");

			if (length < 0)
				throw new ArgumentException("length cannot be less than zero");

			//if (dropdown_style == ComboBoxStyle.DropDownList)
			//	return;

			//textbox_ctrl.Select(start, length);
		}

		public void SelectAll()
		{
			imp.SelectAll();
		}

		protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		{
			bool vertically_anchored = (Anchor & AnchorStyles.Top) != 0 && (Anchor & AnchorStyles.Bottom) != 0;
			bool vertically_docked = Dock == DockStyle.Left || Dock == DockStyle.Right || Dock == DockStyle.Fill;

			if ((specified & BoundsSpecified.Height) != 0 ||
				(specified == BoundsSpecified.None && (vertically_anchored || vertically_docked)))
			{
				//requested_height = height;
				//height = SnapHeight(height);
			}

			base.SetBoundsCore(x, y, width, height, specified);
		}

		protected override void SetItemCore(int index, object value)
		{
			if (index < 0 || index >= Items.Count)
				return;

			Items[index] = value;
		}

		protected override void SetItemsCore(IList value)
		{
			BeginUpdate();
			try
			{
				Items.Clear();
				Items.AddRange(value);
			}
			finally
			{
				EndUpdate();
			}
		}

		public override string ToString()
		{
			return base.ToString() + ", Items.Count:" + Items.Count;
		}

		#endregion Public Methods

		#region Private Methods
		void OnAutoCompleteCustomSourceChanged(object sender, CollectionChangeEventArgs e)
		{
			//FIXME: handle add, remove and refresh events in AutoComplete algorithm.
		}

		void LayoutComboBox()
		{
		}

		private int FindStringCaseInsensitive(string search)
		{
			if (search.Length == 0)
			{
				return -1;
			}

			for (int i = 0; i < Items.Count; i++)
			{
				if (String.Compare(GetItemText(Items[i]), 0, search, 0, search.Length, true) == 0)
					return i;
			}

			return -1;
		}

		// Search in the list for the substring, starting the search at the list 
		// position specified, the search wraps thus covering all the list.
		internal int FindStringCaseInsensitive(string search, int start_index)
		{
			if (search.Length == 0)
			{
				return -1;
			}
			// Accept from first item to after last item. i.e. all cases of (SelectedIndex+1).
			if (start_index < 0 || start_index > Items.Count)
				throw new ArgumentOutOfRangeException(nameof(start_index));

			for (int i = 0; i < Items.Count; i++)
			{
				int index = (i + start_index) % Items.Count;
				if (String.Compare(GetItemText(Items[index]), 0, search, 0, search.Length, true) == 0)
					return index;
			}

			return -1;
		}

		internal override bool IsInputCharInternal(char charCode)
		{
			return true;
		}

		internal override ContextMenu ContextMenuInternal
		{
			get
			{
				return base.ContextMenuInternal;
			}
			set
			{
				base.ContextMenuInternal = value;
				//if (textbox_ctrl != null)
				//{
				//	textbox_ctrl.ContextMenu = value;
				//}
			}
		}

		internal void RestoreContextMenu()
		{
			//textbox_ctrl.RestoreContextMenu();
		}

		void SetSelectedIndex(int value, bool supressAutoScroll)
		{
			if (selected_index == value)
				return;

			if (value <= -2 || value >= Items.Count)
				throw new ArgumentOutOfRangeException(nameof(value));

			selected_index = value;

			if (dropdown_style != ComboBoxStyle.DropDownList)
			{
				//	if (value == -1)
				//		SetControlText(string.Empty, false, supressAutoScroll);
				//	else
				//		SetControlText(GetItemText(Items[value]), false, supressAutoScroll);
			}

			//if (DropDownStyle == ComboBoxStyle.DropDownList)
			//	Invalidate();

			imp?.SelectItem(value);

			OnSelectedValueChanged(EventArgs.Empty);
			OnSelectedIndexChanged(EventArgs.Empty);
			OnSelectedItemChanged(EventArgs.Empty);
		}

		// If no item is currently selected, and an item is found matching the text 
		// in the textbox, then selected that item.  Otherwise the item at the given 
		// index is selected.
		private void FindMatchOrSetIndex(int index)
		{
			int match = -1;
			if (SelectedIndex == -1 && Text.Length != 0)
				match = FindStringCaseInsensitive(Text);
			if (match != -1)
				SetSelectedIndex(match, true);
			else
				SetSelectedIndex(index, true);
		}

		private void UpdatedItems()
		{
			//if (listbox_ctrl != null)
			//{
			//	listbox_ctrl.UpdateLastVisibleItem();
			//	listbox_ctrl.CalcListBoxArea();
			//	listbox_ctrl.Refresh();
			//}
		}

		protected override void RefreshItem(int index)
		{
			// FIXME
		}

		#endregion Private Methods

		internal Rectangle ButtonArea
		{
			get { return Rectangle.Empty; }
		}

		internal Rectangle TextArea
		{
			get { return Rectangle.Empty; }
		}

		internal bool DropDownButtonEntered
		{
			get; set;
		}

		[ListBindableAttribute(false)]
		public class ObjectCollection : IList, ICollection, IEnumerable
		{

			private ComboBox owner;
			internal ArrayList object_items = new ArrayList();

			#region UIA Framework Events

			//NOTE:
			//	We are using Reflection to add/remove internal events.
			//	Class ListProvider uses the events.
			//
			//Event used to generate UIA StructureChangedEvent
			static object UIACollectionChangedEvent = new object();

			internal event CollectionChangeEventHandler UIACollectionChanged
			{
				add { owner.Events.AddHandler(UIACollectionChangedEvent, value); }
				remove { owner.Events.RemoveHandler(UIACollectionChangedEvent, value); }
			}

			internal void OnUIACollectionChangedEvent(CollectionChangeEventArgs args)
			{
				CollectionChangeEventHandler eh
					= (CollectionChangeEventHandler)owner.Events[UIACollectionChangedEvent];
				if (eh != null)
					eh(owner, args);
			}

			#endregion UIA Framework Events

			public ObjectCollection(ComboBox owner)
			{
				this.owner = owner;
			}

			#region Public Properties
			public int Count
			{
				get { return object_items.Count; }
			}

			public bool IsReadOnly
			{
				get { return false; }
			}

			[Browsable(false)]
			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			public virtual object this[int index]
			{
				get
				{
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException(nameof(index));

					return object_items[index];
				}
				set
				{
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException(nameof(index));
					if (value == null)
						throw new ArgumentNullException(nameof(value));

					//UIA Framework event: Item Removed
					OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Remove, object_items[index]));

					object_items[index] = value;

					//UIA Framework event: Item Added
					OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Add, value));

					owner.imp?.SetItemTitle(index, owner.GetItemText(value));

					if (index == owner.SelectedIndex)
					{
						// FIXME: Support for editbox
						//	if (owner.textbox_ctrl == null)
						//		owner.Refresh();
						//	else
						//	{
						//		owner.textbox_ctrl.Text = value.ToString();
						//		owner.textbox_ctrl.SelectAll();
						//	}
					}
				}
			}

			bool ICollection.IsSynchronized
			{
				get { return false; }
			}

			object ICollection.SyncRoot
			{
				get { return this; }
			}

			bool IList.IsFixedSize
			{
				get { return false; }
			}

			#endregion Public Properties

			#region Public Methods
			public int Add(object item)
			{
				int idx;

				idx = AddItem(item, false);
				owner.UpdatedItems();
				return idx;
			}

			public void AddRange(object[] items)
			{
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				foreach (object mi in items)
					AddItem(mi, true);

				if (owner.Sorted)
					Sort();

				owner.UpdatedItems();
			}

			public void Clear()
			{
				owner.selected_index = -1;
				object_items.Clear();
				owner.imp?.RemoveAllItems();

				owner.UpdatedItems();
				owner.Refresh();

				//UIA Framework event: Items list cleared
				OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, null));
			}

			public bool Contains(object value)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				return object_items.Contains(value);
			}

			public void CopyTo(object[] destination, int arrayIndex)
			{
				object_items.CopyTo(destination, arrayIndex);
			}

			void ICollection.CopyTo(Array destination, int index)
			{
				object_items.CopyTo(destination, index);
			}

			public IEnumerator GetEnumerator()
			{
				return object_items.GetEnumerator();
			}

			int IList.Add(object item)
			{
				return Add(item);
			}

			public int IndexOf(object value)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				return object_items.IndexOf(value);
			}

			public void Insert(int index, object item)
			{
				if (index < 0 || index > Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				if (item == null)
					throw new ArgumentNullException(nameof(item));

				owner.BeginUpdate();

				if (owner.Sorted)
					AddItem(item, false);
				else
				{
					object_items.Insert(index, item);
					owner.imp?.InsertItem(owner.GetItemText(item), index);

					//UIA Framework event: Item added
					OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Add, item));
				}

				owner.EndUpdate();  // Calls UpdatedItems
			}

			public void Remove(object value)
			{
				if (value == null)
					return;
				int index = IndexOf(value);
				if (index >= 0)
					RemoveAt(index);
			}

			public void RemoveAt(int index)
			{
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException(nameof(index));

				if (index < owner.SelectedIndex)
					--owner.SelectedIndex;
				else if (index == owner.SelectedIndex)
					owner.SelectedIndex = -1;

				object removed = object_items[index];

				object_items.RemoveAt(index);
				owner.imp?.RemoveItem(index);

				owner.UpdatedItems();

				//UIA Framework event: Item removed
				OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Remove, removed));
			}
			#endregion Public Methods

			#region Private Methods
			private int AddItem(object item, bool suspend)
			{
				// suspend means do not sort as we put new items in, we will do a
				// big sort at the end
				if (item == null)
					throw new ArgumentNullException(nameof(item));

				if (owner.Sorted && !suspend)
				{
					int index = 0;
					foreach (object o in object_items)
					{
						if (String.Compare(item.ToString(), o.ToString()) < 0)
						{
							object_items.Insert(index, item);
							owner.imp?.InsertItem(owner.GetItemText(item), index);

							// If we added the new item before the selectedindex
							// bump the selectedindex by one, behavior differs if
							// Handle has not been created.
							if (index <= owner.SelectedIndex && owner.IsHandleCreated)
								owner.SelectedIndex++;

							//UIA Framework event: Item added
							OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Add, item));

							return index;
						}
						index++;
					}
				}
				object_items.Add(item);
				owner.imp?.AddItem(owner.GetItemText(item));

				//UIA Framework event: Item added
				OnUIACollectionChangedEvent(new CollectionChangeEventArgs(CollectionChangeAction.Add, item));

				return object_items.Count - 1;
			}

			internal void AddRange(IList items)
			{
				foreach (object mi in items)
					AddItem(mi, false);

				if (owner.Sorted)
					Sort();

				owner.UpdatedItems();
			}

			internal void Sort()
			{
				// If the objects the user put here don't have their own comparer,
				// use one that compares based on the object's ToString
				if (object_items.Count > 0 && object_items[0] is IComparer)
					object_items.Sort();
				else
					object_items.Sort(new ObjectComparer(owner));

				if (owner.imp != null)
				{
					owner.imp.RemoveAllItems();
					foreach (object item in object_items)
						owner.imp.AddItem(owner.GetItemText(item));
				}
			}

			private class ObjectComparer : IComparer
			{
				private ListControl owner;

				public ObjectComparer(ListControl owner)
				{
					this.owner = owner;
				}

				#region IComparer Members
				public int Compare(object x, object y)
				{
					return string.Compare(owner.GetItemText(x), owner.GetItemText(y));
				}
				#endregion
			}
			#endregion Private Methods
		}

		internal interface IComboBoxImp
		{
			NSControl Control { get; }
			bool Enabled { set; }
			string Text { get; set; }
			Font Font { set; }
			int IndexOfSelectedItem { get; }
			string SelectedText { get; set; }

			void RemoveAllItems();
			void AddItem(string item);
			void InsertItem(string title, int index);
			void RemoveItem(int index);
			void SelectItem(int value);
			void Release();
			void SetItemTitle(int index, string title);
			void SelectAll();
		}

		internal class DummyImp : IComboBoxImp
		{
			public DummyImp(ComboBox owner) {}

			public NSControl Control { get { return null; } }
			public bool Enabled { set {} }
			public string Text { get; set; }
			public Font Font { set {} }
			public int IndexOfSelectedItem { get; }
			public string SelectedText { get; set; }

			public void RemoveAllItems() {}
			public void AddItem(string title) {}
			public void InsertItem(string title, int index) {}
			public void RemoveItem(int index) {}
			public void SelectItem(int value) {}
			public void Release() {}
			public void SetItemTitle(int index, string title) {}
			public void SelectAll() {}
		}

		internal class PopUpImp : IComboBoxImp
		{
			internal ComboBox owner;
			internal NSPopUpButton popup;

			public PopUpImp(ComboBox owner)
			{
				this.owner = owner;				

				popup = new NSPopUpButton();
				popup.BezelStyle = NSBezelStyle.Rounded;
				popup.Alignment = NSTextAlignment.Natural;
				popup.Enabled = owner.Enabled;
				popup.Font = owner.Font.ToNSFont();
				//popup.PullsDown = true;
				popup.Activated += owner.OnImpSelectedItemChanged;

				foreach (object item in owner.items)
					popup.AddItem(owner.GetItemText(item));

				if (owner.selected_index >= 0 && owner.selected_index < popup.Items().Length)
					popup.SelectItem(owner.selected_index);
			}

			public NSControl Control
			{
				get { return popup; }
			}

			public string Text
			{
				get { return popup?.SelectedItem?.Title; }
				set
				{
					var index = popup.IndexOfItem(value);
					if (index != -1)
						popup.SelectItem(index);
				}
			}

			public string SelectedText
			{
				get { return popup?.SelectedItem?.Title; }
				set { }
			}

			public int IndexOfSelectedItem
			{
				get { return (int)popup.IndexOfSelectedItem; }
			}

			public Font Font
			{
				set { if (value != null) popup.Font = value.ToNSFont(); }
			}

			public bool Enabled
			{
				set { popup.Enabled = value; }
			}

			public void AddItem(string title)
			{
				popup.AddItem(title);
			}

			public void InsertItem(string title, int index)
			{
				popup.InsertItem(title, index);
			}

			public void Release()
			{
				popup.Activated -= owner.OnImpSelectedItemChanged;
				owner = null;
			}

			public void RemoveAllItems()
			{
				popup.RemoveAllItems();
			}

			public void RemoveItem(int index)
			{
				popup.RemoveItem(index);
			}

			public void SelectItem(int index)
			{
				popup.SelectItem(index);
			}

			public void SetItemTitle(int index, string title)
			{
				popup.Items()[index].Title = title;
			}

			public void SelectAll()
			{
			}
		}

		internal class ComboBoxImp : NSComboBoxDataSource, IComboBoxImp
		{
			internal ComboBox owner;
			internal NSComboBox combo;
			internal string textBeforePopUp;
			internal int indexofCompleted = 0;


			public ComboBoxImp(ComboBox owner)
			{
				this.owner = owner;

				combo = new NSComboBox();
				combo.Bordered = false;
				combo.UsesSingleLineMode = false;
				combo.LineBreakMode = NSLineBreakMode.Clipping;
				combo.BezelStyle = NSTextFieldBezelStyle.Rounded;
				combo.Alignment = NSTextAlignment.Natural;
				combo.Enabled = owner.Enabled;
				combo.Font = owner.Font.ToNSFont();
				combo.DoCommandBySelector = ComboDoCommandBySelector;
				combo.Changed += ComboTextFieldChanged;
				combo.WillPopUp += ComboWillPopUp;
				combo.WillDismiss += ComboWillDismiss;
				combo.Completes = true;
				combo.VisibleItems = 10;
				combo.UsesDataSource = true;
				combo.DataSource = this;
				combo.GetCompletions = ComboGetCompletions;

				if (owner.selected_index >= 0 && owner.selected_index < combo.Count)
					combo.SelectItem(owner.selected_index);
					
				combo.SelectionChanged += ComboSelectionChanged;
			}

			public NSControl Control
			{
				get { return combo; }
			}

			public string Text
			{
				get { return combo?.StringValue; }
				set { combo.StringValue = value; }
			}

			public string SelectedText
			{
				get { return combo.SelectedValue.ToString(); }
				set { combo.StringValue = value; }
			}

			public int IndexOfSelectedItem
			{
				get { return (int)combo.SelectedIndex == -1 ? indexofCompleted : (int)combo.SelectedIndex; }
			}

			public Font Font
			{
				set { if (value != null) combo.Font = value.ToNSFont(); }
			}

			public bool Enabled
			{
				set { combo.Enabled = value; }
			}

			public void AddItem(string title)
			{
			}

			public void InsertItem(string title, int index)
			{
			}

			public void Release()
			{
				combo.WillDismiss -= ComboWillDismiss;
				combo.WillPopUp -= ComboWillPopUp;
				combo.SelectionChanged -= ComboSelectionChanged;
				combo.Changed -= ComboTextFieldChanged;
				combo.Activated -= owner.OnImpSelectedItemChanged;
				owner = null;
			}

			public void RemoveAllItems()
			{
			}

			public void RemoveItem(int index)
			{
			}

			public void SelectItem(int value)
			{
				combo.SelectItem(value);
			}

			public void SetItemTitle(int index, string title)
			{
			}

			public void SelectAll()
			{
				combo.SelectText(combo);
			}

			// Internals

			internal bool ComboDoCommandBySelector(NSControl control, NSTextView textView, Selector selector)
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

			internal void SendWmKey(VirtualKeys key, IntPtr lParam)
			{
				XplatUI.SendMessage(owner.Handle, Msg.WM_KEYDOWN, (IntPtr)key, lParam);

				if (owner.IsHandleCreated) // The previous line could have caused disposing the control (esc, enter, ...)
					XplatUI.SendMessage(owner.Handle, Msg.WM_KEYUP, (IntPtr)key, lParam);
			}

			internal void ComboTextFieldChanged(object sender, EventArgs e)
			{
				owner.BeginInvoke((Action)delegate
				{
					owner.OnImpSelectedItemChanged(sender, e);

					owner.OnTextUpdate(e);
					owner.OnTextChanged(e);

					combo.ReloadData();
				});
			}

			internal void ComboSelectionChanged(object sender, EventArgs e)
			{
				indexofCompleted = -1;
				owner.BeginInvoke((Action)delegate
				{
					owner.OnImpSelectedItemChanged(sender, e);
					owner.OnTextChanged(e);
				});
			}

			internal void ComboWillPopUp(object sender, EventArgs e)
			{
				textBeforePopUp = combo.StringValue;
				combo.ReloadData();
			}

			internal void ComboWillDismiss(object sender, EventArgs e)
			{
				owner.BeginInvoke((Action)delegate
				{
					if (textBeforePopUp != combo.StringValue)
						owner.OnTextChanged(e);
				});
			}

			[Export("comboBox:objectValueForItemAtIndex:")]
			public override NSObject ObjectValueForItem(NSComboBox comboBox, nint index)
			{
				var item = owner.items[(int)index];
				var title = owner.GetItemText(item);
				return (NSString)title;
			}

			[Export("numberOfItemsInComboBox:")]
			public override nint ItemCount(NSComboBox comboBox)
			{
				return owner.items.Count;
			}

			[Export("comboBox:indexOfItemWithStringValue:")]
			public override nint IndexOfItem(NSComboBox comboBox, string value)
			{
				for (int i = 0; i < owner.items.Count; ++i)
				{
					var item = owner.items[i];
					var title = owner.GetItemText(item);
					if (title == value)
						return (nint)i;
				}
				return - 1;
			}

			[Export("comboBox:completedString:")]
			public override string CompletedString(NSComboBox comboBox, string prefix)
			{
				int i = 0;
				foreach (var item in owner.items)
				{
					var title = owner.GetItemText(item);
					if (title.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
					{
						indexofCompleted = i;
						BeginInvokeOnMainThread(() => { DidCallCompletedString(prefix, indexofCompleted); });
						return title;
					}
					++i;
				}

				indexofCompleted = -1;
				BeginInvokeOnMainThread(() => { DidCallCompletedString(prefix, indexofCompleted); });
				return prefix;
			}

			public virtual string[] ComboGetCompletions(NSControl control, NSTextView textView, string[] words, NSRange charRange, ref nint index)
			{
				var prefix = textView.String?.Substring(0, (int)charRange.Location + (int)charRange.Length) ?? String.Empty;

				var completions = new List<string>();
				foreach (var item in owner.items)
				{
					var title = owner.GetItemText(item);
					if (title.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
						completions.Add(title.Substring((int)charRange.Location));
				}
				index = -1;
				return completions.ToArray();
			}

			protected virtual void DidCallCompletedString(string prefix, int indexOfCompleted)
			{
				// Open pop-up with auto completions, if completion found
				if (indexOfCompleted != -1 && combo.Cell != null && !combo.Cell.AccessibilityExpanded)
					combo.Cell.AccessibilityExpanded = true;
			}
		}
	}
}

#endif

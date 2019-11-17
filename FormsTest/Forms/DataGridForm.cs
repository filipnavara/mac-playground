using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MailClient.Collections;
using MailClient.Common.UI.Controls.ControlDataGrid;

namespace FormsTest
{
	public class DataGridForm : Form
	{
		IContainer components;
		ControlDataGrid grid;
		BindableList<Row> data;

		public DataGridForm()
		{
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		void InitializeComponent()
		{
			SuspendLayout();

			Size = new Size(800, 600);
			Padding = new Padding(8);

			grid = new ControlDataGrid();
			grid.Font = new Font(Font.SystemFontName, 10);
			grid.AutomaticallyResizeColumnsToFit = false;
			grid.BorderStyle = BorderStyle.FixedSingle;
			grid.MultiSelect = true;
			grid.Name = "grid";
			grid.Dock = DockStyle.Fill;
			grid.AutoHideHorizontalScrollbar = true;
			grid.Margin = new Padding(4, 4, 4, 4);
			grid.UseOddItemBackColor = true;
			grid.OddItemBackColor = Color.FromArgb(255, 240, 240, 240);
			grid.BackColor = Color.White;

			DataGridColumn column;
			column = new DataGridColumn("Selected", "Selected", 24);
			column.EditMode = EditModeType.Editable;
			//column.DisplayMode = DataGridColumn.DisplayModeType.ImageOnly;
			column.HorizontalAlign = HorizontalAlignment.Center;
			column.ContentHorizontalAlign = HorizontalAlignment.Center;
			column.ContentVerticalAlign = VerticalAlignment.Center;
			column.SetBindingCheckBox("Selected");
			column.AllowSelection = true;
			column.UseFixedWidth = true;

			grid.Columns.Add(column);

			column = new DataGridColumn("IsNeutral", "Neutral", 24);
			column.Binding = "IsNeutralCulture";
			//column.DisplayMode = DataGridColumn.DisplayModeType.ImageOnly;
			column.HorizontalAlign = HorizontalAlignment.Center;
			column.ContentHorizontalAlign = HorizontalAlignment.Center;
			column.ContentVerticalAlign = VerticalAlignment.Center;
			column.SetBindingCheckBox("IsNeutralCulture");
			column.AllowSelection = false;
			column.UseFixedWidth = true;
			//grid.Columns.Add(column);

			column = new DataGridColumn("DisplayName", "Display Name", 140);
			column.Binding = "DisplayName";
			grid.Columns.Add(column);

			column = new DataGridColumn("EnglishName", "English Name", 120);
			column.Binding = "EnglishName";
			grid.Columns.Add(column);

			column = new DataGridColumn("NativeName", "Native Name", 130);
			column.Binding = "NativeName";
			grid.Columns.Add(column);

			column = new DataGridColumn("DisplayName", "Display Name", 140);
			column.Binding = "DisplayName";
			grid.Columns.Add(column);

			column = new DataGridColumn("EnglishName", "English Name", 120);
			column.Binding = "EnglishName";
			grid.Columns.Add(column);

			column = new DataGridColumn("NativeName", "Native Name", 130);
			column.Binding = "NativeName";
			grid.Columns.Add(column);

			Controls.Add(grid);

			ResumeLayout(false);
			PerformLayout();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			data = CreateDataSource();
			grid.SetDataSource(data);
		}

		BindableList<Row> CreateDataSource()
		{
			var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
			var list = new BindableList<Row>();
			foreach (var culture in cultures)
				list.Add(new Row { Culture = culture });

			return list;
		}
	}

	class Row : IComparable
	{
		public bool Selected { get; set; }
		public CultureInfo Culture { get; set; }
		public bool IsNeutralCulture { get { return Culture.IsNeutralCulture; } }
		public string DisplayName { get { return Culture.DisplayName; } }
		public string EnglishName { get { return Culture.EnglishName; } }
		public string NativeName{ get { return Culture.NativeName; } }

		public int CompareTo(object obj)
		{
			return DisplayName.CompareTo(((Row)obj).DisplayName);
		}
	}
}

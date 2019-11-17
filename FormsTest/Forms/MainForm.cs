using System;
//using MailClient.Common.UI;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace FormsTest
{
	using System.Collections.Generic;
	//using MailClient.UI.Forms;
	//using CocoaMessageBox = MessageBox;
	using MessageBox = System.Windows.Forms.MessageBox;

	public partial class MainForm : Form
    {
        const string title = "Title";
        const string loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";

        Pair[] Dialogs = {
            new Pair { Name = "Color Dialog", Action = delegate { new ColorDialog().ShowDialog(); } },
            new Pair { Name = "Open File", Action = delegate { new OpenFileDialog().ShowDialog(); } },
            new Pair { Name = "Save File", Action = delegate { 
                new SaveFileDialog().ShowDialog(); } },
            new Pair { Name = "Choose Folder", Action = delegate { 
					var dlg = new FolderBrowserDialog();
					dlg.Description = "Vyber nějakou složku vosle";
					dlg.SelectedPath = "/Library";
					var result = dlg.ShowDialog();
					Debug.WriteLine(result == DialogResult.OK ? "Vybrals " + dlg.SelectedPath : "Nic nevybrals" );
			} },
            new Pair { Name = "Font Dialog", Action = delegate { new FontDialog().ShowDialog(); } },
            new Pair { Name = "Page Setup", Action = delegate { new PageSetupDialog().ShowDialog(); } },
            new Pair { Name = "MB Info OK", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.OK, MessageBoxIcon.Information); } },
            new Pair { Name = "MB Warn OK/C", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning); } },
            new Pair { Name = "MB Warn Y/N", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning); } },
            new Pair { Name = "MB Error Y/N/C", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error); } },
            new Pair { Name = "MB Quest A/R/I", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Question); } },
            new Pair { Name = "MB Error R/C", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error); } },
			new Pair { Name = "MB Error C/R", Action = delegate { MessageBox.Show(loremIpsum, title, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2); } },
		};

        public delegate void Action();
        public class Pair
        {
            public string Name { get; set; }
            public Action Action { get; set; }
        }

        private string filter = String.Empty;

        public MainForm()
        {
            //DisplaySettingsManager.Initialize();

            InitializeComponent();

            UpdateList();
            InitDialogCombo();

            // Mac layout debugging
            //this.DebugAllControls();

            this.filterTextBox.KeyDown += FilterTextBox_KeyDown;

			AddButton("WebForm", () => { new WebForm().Show(); });
            AddButton("ImageForm", () => { new ImageForm().Show(); });
			AddButton("TextBoxes", () => { new TextBoxForm().Show(); });
			//AddButton("ImapOptions", () => { new ImapOptionsForm().Show(); });
			//AddButton("MailOptions", () => { new MailOptionsForm().Show(); });
			/*AddButton("FormInputBox", () => { new FormInputBox(
				"Nazdar", 
				"Přihlášení k webovému kalendáři",
			    "Zadej url kalendáře, ke kterému se chceš přihlásit, třeba http://calndar.org-mycalndar.ics"
			).Show(); });*/
			//AddButton("Layout 1", () => { new DebugLayoutForm().Show(); });
			//AddButton("Layout 2", () => { new DebugLayoutForm2().Show(); });
			//AddButton("Layout 3", () => { new DebugLayoutForm3().Show(); });
			//AddButton("Layout 4", () => { new DebugLayoutForm4().Show(); });
			/*AddButton("Editor", () =>
			{
				var f = new Form() { Size = new Size(500, 300), Text = "Editor" };

				f.Controls.Add(new HtmlEditorControl.HtmlEditorBrowser { Dock = DockStyle.Fill });
				f.Controls.Add(new MailClient.Common.UI.Controls.ControlTextBox.ControlTextBox { Dock = DockStyle.Top });
				var toolStrip = new MailClient.Common.UI.Controls.ControlToolStrip.ControlToolStrip { Dock = DockStyle.Top, AutoSize = true, Height = 40 };
				f.Controls.Add(toolStrip);
				toolStrip.Items.Add(new MailClient.Common.UI.Controls.ToolStripControls.ControlToolStripButtonFontSelector());
				f.Show();
			});*/
			/*AddButton("FormLinkMessageBox", () =>
			{
				new FormLinkMessageBox("Hello", "Zkušební doba vypršela. Platnou licenci pro eM Client můžete získat na http://cz.emclient.com/prehled-cenovych-moznosti\r\nNyní budete mít ke svým datům přístup pouze v režimu off-line. Synchronizace a odesílání jakýchkoliv zpráv bude nyní zablokována. Jakmile bude zadána validní licence, všechny funkce aplikace budou okamžitě obnoveny.").Show();
			});*/
			/*AddButton("Recurrence", () =>
			{
				var f = new Form() { Size = new Size(500, 300), Text = "Recurrence" };
				f.Controls.Add(new MailClient.UI.Controls.ControlRecurrence());
				f.Show();
			});
			AddButton("Confirmations", () =>
			{
				var f = new Form() { Size = new Size(400, 500), Text = "Confirmations" };
				f.Controls.Add(new MailClient.UI.Controls.SettingsControls.ControlSettingsConfirmations() { Dock = DockStyle.Top, AutoSize = true });
				f.DebugAllControls();
				f.Show();
			});
			AddButton("Spell checker", () =>
			{
				var f = new Form() { Size = new Size(400, 500), Text = "Spell checker" };
				f.Controls.Add(new MailClient.UI.Controls.SettingsControls.ControlSettingsSpellChecker() { Dock = DockStyle.Top, AutoSize = true });
				f.Show();
			});
			AddButton("Autodiscover", () =>
			{
				var f = new Form() { Size = new Size(400, 500), Text = "Autodiscover" };
				f.Controls.Add(new MailClient.UI.Controls.WizardControls.ControlExpandablePanelAutodiscover() { Dock = DockStyle.Top, AutoSize = true, Expanded = true });
				f.Show();
			});*/

			AddButton("Animations", () => { new AnimationsForm().Show(); });
			AddButton("Print dialog", () => { ShowPrintDialog(); });
			AddButton("Toolstrip form", () => { new ToolstripForm().Show(); });
#if MAC
			AddButton("Bg activity", () => { ToggleBgActivity(); });
			AddButton("Swizzle (cls)", () => { SwizzleCls(); });
			AddButton("NSException", () => { NativeException(); });
			AddButton("QuickLook panel", () => { ToggleQuickLookPanel(); });
			AddButton("NC Preferences", () => { ReadNotificationCenterPreferences(); });
			AddButton("Back color", () => { ChangeBackColor(); });
#endif
		}

		List<Button> buttons = new List<Button>();
		void AddButton(string text, Action a)
		{
			const int gap = 5;

			var b = new Button();
			b.AutoSize = true;
			b.Anchor = AnchorStyles.Right;
			b.Click += (sender, e) => { a(); };
			b.Text = text;
			b.SetBounds(panel1.Width - b.Width - gap, gap + buttons.Count * 28, 0, 0, BoundsSpecified.Y | BoundsSpecified.X);

			buttons.Add(b);
			panel1.Controls.Add(b);
		}

		private void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"KeyDown:[KeyCode={e.KeyCode},KeyValue={e.KeyValue},Modifiers={e.Modifiers}]");
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        void InitDialogCombo()
        {
            dialogTypeCombo.ValueMember = "Action";
            dialogTypeCombo.DisplayMember = "Name";
            dialogTypeCombo.DataSource = Dialogs;
        }

        void UpdateList()
        {
            ListFontFamilies(listBox1, filter);
        }

		FontCollection fonts = new InstalledFontCollection();

		void ListFontFamilies(ListBox listBox, string filter)
        {
            var families = fonts.Families.Select(x => x.Name).ToArray();

            Array.Sort(families);

			listBox.BeginUpdate();
            listBox1.Items.Clear();
			foreach (var family in families)
            {
                if (String.IsNullOrEmpty(filter) || -1 != family.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase))
                    listBox.Items.Add(family);
            }
			listBox.EndUpdate();
        }

        private void listBox1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                string name = listBox1.SelectedItem as string;
                label2.Font = new Font(name, label2.Font.Size);
                label3.Font = new Font(name, label3.Font.Size);
                label4.Font = new Font(name, label4.Font.Size);
                label5.Font = new Font(name, label5.Font.Size);
                label7.Font = new Font(name, label7.Font.Size);
            }
            catch
            {
            }
        }

        //		protected override void OnPaint (PaintEventArgs e)
        //		{
        //			NSView nsView = (NSView)MonoMac.ObjCRuntime.Runtime.GetNSObject(this.Handle);
        //			if (nsView != null && nsView.LockFocusIfCanDraw ()) {
        //				base.OnPaint (e);
        //				nsView.UnlockFocus ();
        //			}
        //		}

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            //var form = new LayoutForm();
            //form.Show();
        }

        private void button3_Click(object sender, System.EventArgs e)
        {
            try
            {
                (dialogTypeCombo.SelectedValue as Action)();
            }
			catch (Exception x)
            {
				Debug.WriteLine(x.ToString());
            }
        }

        private void filterTextBox_TextChanged(object sender, EventArgs e)
        {
            filter = filterTextBox.Text;
            UpdateList();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            Console.WriteLine(e);

            base.OnKeyDown(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg >= (int)Msg.WM_KEYFIRST && m.Msg <= (int)Msg.WM_KEYLAST)
                Console.WriteLine("MainForm.WndProc({0})", m);

            base.WndProc(ref m);
        }

		void ShowPrintDialog()
		{

			//https://github.com/brunophilipe/Noto/blob/master/Noto/View%20Controllers/Printing/PrintingView.swift
			//https://nshipster.com/uiprintinteractioncontroller/
			//https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/Printing/osxp_printingapi/osxp_printingapi.html#//apple_ref/doc/uid/10000083i-CH2-SW2
			//https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/Printing/osxp_printapps/osxp_printapps.html#//apple_ref/doc/uid/20000861-BAJBFGED
			//https://developer.apple.com/documentation/appkit/nsprintinfo?language=objc
			var dialog = new PrintDialog();
			//dialog.PrinterSettings = 
			dialog.ShowDialog(this);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			e.Cancel = true;
			Visible = false;
		}

		AppKit.NSTitlebarAccessoryViewController tbc;
		void ChangeBackColor()
		{
			var view = ObjCRuntime.Runtime.GetNSObject<AppKit.NSView>(Handle);
			var window = view.Window;

			if (!window.TitlebarAppearsTransparent)
			{
				window.TitlebarAppearsTransparent = true;
				window.StyleMask |= AppKit.NSWindowStyle.TexturedBackground;
				window.BackgroundColor = AppKit.NSColor.Red;

				//var box = new AppKit.NSBox(CoreGraphics.CGRect.FromLTRB(0, 0, 100, 20));
				//box.FillColor = AppKit.NSColor.Clear;
				//tbc = new AppKit.NSTitlebarAccessoryViewController();
				//tbc.View = box;
				//window.AddTitlebarAccessoryViewController(tbc);
			}
			else
			{
				window.TitlebarAppearsTransparent = false;
				window.StyleMask &= ~AppKit.NSWindowStyle.TexturedBackground;
				window.BackgroundColor = AppKit.NSColor.WindowBackground;
				//window.RemoveTitlebarAccessoryViewControllerAtIndex(0);
			}
		}
	}
}

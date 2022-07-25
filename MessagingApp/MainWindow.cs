using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingApp
{
    internal class MainWindow : Form
    {
        private Panel _messageArea;

        private TextBox _textBox;

        private Label _textBoxLabel;

        private Button _sendButton;

        private int _newMessagePosition = 0;

        internal MainWindow()
        {
            ClientSize = new Size(1024, 768);
            MaximumSize = MinimumSize = Size;
            BackColor = Color.DarkGray;

            _newMessagePosition = 0;

            _messageArea = new Panel();
            _messageArea.Location = new Point(0, 10);
            _messageArea.ClientSize = new Size(1024, 650);
            _messageArea.BackColor = Color.LightGray;
            _messageArea.AutoScroll = true;

            Controls.Add(_messageArea);

            _textBox = new TextBox();
            _textBox.Location = new Point(2, 662);
            _textBox.Size = new Size(900, 104);
            _textBox.BackColor = Color.White;
            _textBox.Multiline = true;
            _textBox.ScrollBars = ScrollBars.Vertical;
            
            _textBox.GotFocus += OnTextFocus;
            _textBox.TextChanged += OnTextChanged;
            _textBox.LostFocus += OnTextUnfocus;

            _textBoxLabel = new Label();
            _textBoxLabel.Enabled = false;
            _textBoxLabel.Text = "Type a message...";

            _textBox.Controls.Add(_textBoxLabel);

            Controls.Add(_textBox);

            _sendButton = new Button();
            _sendButton.Location = new Point(902, 662);
            _sendButton.Size = new Size(120, 104);
            _sendButton.Text = "Send";
            _sendButton.Click += OnSendClick;
            _sendButton.Enabled = false;

            Controls.Add(_sendButton);
        }

        private void OnTextFocus(object? sender, EventArgs e)
        {
            if (sender == null)
                return;

            _textBoxLabel.Visible = false;
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (sender == null)
                return;

            TextBox textBox = (TextBox)sender;

            _sendButton.Enabled = !string.IsNullOrWhiteSpace(textBox.Text);
        }

        private void OnTextUnfocus(object? sender, EventArgs e)
        {
            if (sender == null)
                return;

            TextBox textBox = (TextBox)sender;

            _textBoxLabel.Visible = string.IsNullOrEmpty(textBox.Text);
        }

        private void OnSendClick(object? sender, EventArgs e)
        {
            if (sender == null)
                return;

            RegisterMessage(true, _textBox.Text);
            _textBox.Text = "";
        }

        private void RegisterMessage(bool myMessage, string message)
        {
            Label label = new Label();

            if (myMessage)
                label.BackColor = Color.LightGreen;
            else
                label.BackColor = Color.PaleVioletRed;
            
            label.Text = message;

            label.MinimumSize = new Size(1000, 0);
            label.MaximumSize = new Size(1000, int.MaxValue);

            label.Size = label.PreferredSize;

            label.Location = new Point(0, _newMessagePosition);

            _newMessagePosition += label.Size.Height + 2;

            _messageArea.Controls.Add(label);
        }
    }
}

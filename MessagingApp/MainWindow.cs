using System.ComponentModel;
using System.Diagnostics;

namespace MessagingApp
{
    internal class MainWindow : Form
    {
        private readonly NetworkBase _network;

        private Panel _messageArea;

        private TextBox _textBox;

        private Button _sendButton;

        private int _newMessagePosition = 0;

        internal MainWindow(NetworkBase network)
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
            _textBox.TextChanged += OnTextChanged;

            _textBox.PlaceholderText = "Type a message...";
            
            _textBox.KeyDown += OnKeyDown;
            _textBox.KeyUp += OnKeyUp;

            Controls.Add(_textBox);

            _sendButton = new Button();
            _sendButton.Location = new Point(902, 662);
            _sendButton.Size = new Size(120, 104);
            _sendButton.Text = "Send\nCtrl + Enter";
            _sendButton.Click += OnSendClick;
            _sendButton.Enabled = false;

            Controls.Add(_sendButton);

            _network = network;
            _network.Start(OnMessageReceived);
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (sender == null)
                return;

            TextBox textBox = (TextBox)sender;

            _sendButton.Enabled = !string.IsNullOrWhiteSpace(textBox.Text);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if ((e.KeyData & Keys.Control) == Keys.Control)
                e.SuppressKeyPress = true;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.KeyData);

            if (e.KeyData == (Keys.Return | Keys.Control))
            {
                Debug.WriteLine($"Enabled: {_sendButton.Enabled}");

                e.SuppressKeyPress = true;

                if(_sendButton.Enabled)
                    _sendButton.PerformClick();
            }
        }

        private void OnSendClick(object? sender, EventArgs e)
        {
            if (sender == null)
                return;

            RegisterMessage(true, 0, _textBox.Text);
            _network.SendMessage(_textBox.Text);
            _textBox.Text = "";
        }

        private void OnMessageReceived(int senderID, string message)
        {
            Invoke(RegisterMessage, false, senderID, message);
        }

        internal void RegisterMessage(bool myMessage, int senderID, string message)
        {
            Label label = new Label();

            if (myMessage)
            {
                label.BackColor = Color.LightGreen;
                label.Text = message;
            }
            else
            {
                label.BackColor = Color.PaleVioletRed;
                label.Text = $"({senderID}): {message}";

            }


            label.MinimumSize = new Size(1000, 0);
            label.MaximumSize = new Size(1000, int.MaxValue);

            label.Size = label.PreferredSize;

            label.Location = new Point(0, _newMessagePosition);

            _newMessagePosition += label.Size.Height + 2;

            _messageArea.Controls.Add(label);
        }
    }
}

namespace MessagingApp
{
    internal class ConnectionWindow : Form
    {
        private Action _closeMain;

        internal ConnectionWindow(Action closeMain, bool isServer)
        {
            LostFocus += OnRecoverFocus;
            _closeMain = closeMain;
            Disposed += OnClosed;
        }

        private void OnRecoverFocus(object? sender, EventArgs e)
        {
            Focus();
        }
        
        private void OnClosed(object? sender, EventArgs e)
        {
            _closeMain.Invoke();
        }
    }
}

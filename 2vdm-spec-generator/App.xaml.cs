namespace _2vdm_spec_generator
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                InitializeComponent();
                MainPage = new AppShell(); // Shellを構築
            }
            catch (Exception ex)
            {
                // エラーメッセージをポップアップで表示
                MainPage = new ContentPage
                {
                    Content = new VerticalStackLayout
                    {
                        Children =
                        {
                            new Label { Text = "アプリケーションの初期化中にエラーが発生しました:" },
                            new Label { Text = ex.Message },
                            new Label { Text = ex.StackTrace }
                        }
                    }
                };
            }
        }
    }
}
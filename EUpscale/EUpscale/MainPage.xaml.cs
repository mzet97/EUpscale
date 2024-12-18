using CommunityToolkit.Maui.Storage;

namespace EUpscale
{
    public partial class MainPage : ContentPage
    {
        string _photoPath;
        string _upscaledPhotoPath;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await SolicitarPermissoesAsync();
        }

        private async Task SolicitarPermissoesAsync()
        {
            var statusCamera = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (statusCamera != PermissionStatus.Granted)
            {
                statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
            }

            var statusArmazenamento = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (statusArmazenamento != PermissionStatus.Granted)
            {
                statusArmazenamento = await Permissions.RequestAsync<Permissions.StorageWrite>();
            }

            if (statusCamera != PermissionStatus.Granted || statusArmazenamento != PermissionStatus.Granted)
            {
                await DisplayAlert("Permissões Necessárias", "O aplicativo precisa de permissão para acessar a câmera e o armazenamento.", "OK");
            }
        }


        private async void OnCapturePhotoClicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                _photoPath = photo.FullPath;
                CapturedImage.Source = ImageSource.FromFile(_photoPath);
            }
        }

        private async void OnUpscaleImageClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_photoPath))
            {
                if (double.TryParse(ScaleFactorEntry.Text, out double scaleFactor) && scaleFactor > 0)
                {
                    _upscaledPhotoPath = Path.Combine(FileSystem.CacheDirectory, "upscaled_photo.jpg");

                    try
                    {
                        ProcessingIndicator.IsVisible = true;
                        ProcessingIndicator.IsRunning = true;

                        await Task.Run(() =>
                        {
                            ImageUpscalerParallel.UpscaleImageNewtonOptimizedJpg(_photoPath, _upscaledPhotoPath, scaleFactor);
                        });

                        CapturedImage.Source = ImageSource.FromFile(_upscaledPhotoPath);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Erro", $"Falha ao aplicar upscaling: {ex.Message}", "OK");
                    }
                    finally
                    {
                        ProcessingIndicator.IsVisible = false;
                        ProcessingIndicator.IsRunning = false;
                    }
                }
                else
                {
                   await DisplayAlert("Erro", "Por favor, insira um valor de escala válido maior que zero.", "OK");
                }
                await DisplayAlert("Sucesso", "Finalizado...", "OK");
            }
            else
            {
                await DisplayAlert("Erro", "Nenhuma foto capturada.", "OK");
            }
        }

        private async void OnSaveImageClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_upscaledPhotoPath))
            {
                var fileName = Path.GetFileName(_upscaledPhotoPath);
                if (!string.IsNullOrEmpty(_upscaledPhotoPath))
                {
                    using var stream = File.OpenRead(_upscaledPhotoPath);
                    var result = await FileSaver.Default.SaveAsync(fileName, stream);
                    if (result.IsSuccessful)
                    {
                        await DisplayAlert("Sucesso", "Imagem salva com sucesso!", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Falha ao salvar imagem.", "OK");
                    }
                }
                
            }
            else
            {
                await DisplayAlert("Erro", "Nenhuma imagem upscalada para salvar.", "OK");
            }
        }
    }

}

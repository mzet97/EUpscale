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
                        // Mostrar o indicador de processamento
                        ProcessingIndicator.IsVisible = true;
                        ProcessingIndicator.IsRunning = true;

                        await Task.Run(() =>
                        {
                            ImageUpscalerParallel.UpscaleImageNewtonOptimizedJpg(_photoPath, _upscaledPhotoPath, scaleFactor);
                        });

                        // Atualizar a imagem processada na interface
                        CapturedImage.Source = ImageSource.FromFile(_upscaledPhotoPath);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Erro", $"Falha ao aplicar upscaling: {ex.Message}", "OK");
                    }
                    finally
                    {
                        // Esconder o indicador de processamento
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

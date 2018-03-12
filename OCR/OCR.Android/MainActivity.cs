using Android.App;
using Android.Widget;
using Android.OS;
//using Com.Abbyy.Mobile.Ocr4;
using Com.Abbyy.Mobile.Rtr;
using Android.Hardware;
using Android.Graphics;
using Android.Views;
//using Android.Widget;
using System.Collections.Generic;
using Java.Lang;
using Android.Util;
using Android.Content;
using Android.Preferences;
using System.Threading.Tasks;
using Android.Content.PM;

namespace OCR.Droid
{
    [Activity(Label = "OCR", MainLauncher = true, Icon = "@mipmap/icon", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : Activity
    {
        //int count = 1;
        // Licensing
        private static string licenseFileName = "AbbyyRtrSdk.license";

        ///////////////////////////////////////////////////////////////////////////////
        // Some application settings that can be changed to modify application behavior:
        // The camera zoom. Optically zooming with a good camera often improves results
        // even at close range and it might be required at longer ranges.
        private static int cameraZoom = 1;
        // The default behavior in this sample is to start recognition when application is started or
        // resumed. You can turn off this behavior or remove it completely to simplify the application
        private static bool startRecognitionOnAppStart = true;
	// Area of interest specified through margin sizes relative to camera preview size
	private static int areaOfInterestMargin_PercentOfWidth = 4;
        private static int areaOfInterestMargin_PercentOfHeight = 25;
        // A subset of available languages shown in the UI. See all available languages in Language enum.
        // To show all languages in the UI you can substitute the list below with:
        private Language[] languages = {
            Language.ChineseSimplified,
            Language.ChineseTraditional,
            Language.English,
            Language.French,
            Language.German,
            Language.Italian,
            Language.Japanese,
            Language.Korean,
            Language.Polish,
            Language.PortugueseBrazilian,
            Language.Russian,
            Language.Spanish,
        };
        ///////////////////////////////////////////////////////////////////////////////

        // The 'Abbyy RTR SDK Engine' and 'Text Capture Service' to be used in this sample application
        private Engine engine;
        public ITextCaptureService textCaptureService;

        // The camera and the preview surface
        public Android.Hardware.Camera camera;
        public SurfaceViewWithOverlay surfaceViewWithOverlay;
        public ISurfaceHolder previewSurfaceHolder;
        private SurfaceHolderCallbackCus surfaceCallback;
        private CameraAutoFocusCallbackCus simpleCameraAutoFocusCallback;
        private TextCaptureServiceCallbackCus textCaptureCallback;
        private CameraPreviewCallbackCus cameraPreviewCallback;
        private CameraAutoFocusCallbackCus startRecognitionCameraAutoFocusCallback;
        private CameraAutoFocusCallbackCus finishCameraInitialisationAutoFocusCallback;

        // Actual preview size and orientation
        private Android.Hardware.Camera.Size cameraPreviewSize;
        private int orientation;

        // Auxiliary variables
        private bool inPreview = false; // Camera preview is started
        public bool stableResultHasBeenReached; // Stable result has been reached
        public bool startRecognitionWhenReady; // Start recognition next time when ready (and reset this flag)
        private Handler handler = new Handler(); // Posting some delayed actions;

        // UI components
        public Button startButton; // The start button
        public TextView warningTextView; // Show warnings from recognizer
        public TextView errorTextView; // Show errors from recognizer

        // Text displayed on start button
        public static string BUTTON_TEXT_START = "Start";
        private static string BUTTON_TEXT_STOP = "Stop";
        private static string BUTTON_TEXT_STARTING = "Starting...";

        private static string CameraAutoFocusCallbackStart = "Start";
        private static string CameraAutoFocusCallbackFinish = "Finish";
        private static string CameraAutoFocusCallbackSimple = "Simple";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            simpleCameraAutoFocusCallback = new CameraAutoFocusCallbackCus(this, CameraAutoFocusCallbackSimple);
            startRecognitionCameraAutoFocusCallback = new CameraAutoFocusCallbackCus(this, CameraAutoFocusCallbackStart);
            finishCameraInitialisationAutoFocusCallback = new CameraAutoFocusCallbackCus(this, CameraAutoFocusCallbackFinish);
            textCaptureCallback = new TextCaptureServiceCallbackCus(this);
            cameraPreviewCallback = new CameraPreviewCallbackCus(this);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            //Button button = FindViewById<Button>(Resource.Id.myButton);

            //button.Click += delegate { button.Text = $"{count++} clicks!"; };
            //SetContentView(R.layout.activity_main);

            // Retrieve some ui components
            warningTextView = (TextView)FindViewById(Resource.Id.warningText);
            errorTextView = (TextView)FindViewById(Resource.Id.errorText);
            startButton = (Button)FindViewById(Resource.Id.startButton);
            startButton.Click += StartButton_Click;

            // Initialize the recognition language spinner
            initializeRecognitionLanguageSpinner();
            // Manually create preview surface. The only reason for this is to
            // avoid making it public top level class
            RelativeLayout layout = (RelativeLayout)startButton.Parent;

            surfaceViewWithOverlay = new SurfaceViewWithOverlay(this);
            RelativeLayout.LayoutParams params1 = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);
            surfaceViewWithOverlay.LayoutParameters = params1;
            // Add the surface to the layout as the bottom-most view filling the parent
            layout.AddView(surfaceViewWithOverlay, 0);

            // Create text capture service
            if (createTextCaptureService())
            {
                surfaceCallback = new SurfaceHolderCallbackCus(this);
                // Set the callback to be called when the preview surface is ready.
                // We specify it as the last step as a safeguard so that if there are problems
                // loading the engine the preview will never start and we will never attempt calling the service
                surfaceViewWithOverlay.Holder.AddCallback(surfaceCallback);
            }

            layout.Click += Layout_Click;
        }
        private void StartButton_Click(object sender, System.EventArgs e)
        {
            onStartButtonClick((View)sender);
        }

        private void Layout_Click(object sender, System.EventArgs e)
        {
            // if BUTTON_TEXT_STARTING autofocus is already in progress, it is incorrect to interrupt it
            if (!startButton.Text.Equals(BUTTON_TEXT_STARTING))
            {
                autoFocus(simpleCameraAutoFocusCallback);
            }
        }


        // Checks that FOCUS_MODE_CONTINUOUS_VIDEO supported
        public bool isContinuousVideoFocusModeEnabled(Android.Hardware.Camera camera)
        {
            return camera.GetParameters().SupportedFocusModes.Contains(Android.Hardware.Camera.Parameters.FocusModeContinuousVideo);
        }

        // Sets camera focus mode and focus area
        public void setCameraFocusMode(string mode)
        {
            // Camera sees it as rotated 90 degrees, so there's some confusion with what is width and what is height)
            int width = 0;
            int height = 0;
            int halfCoordinates = 1000;
            int lengthCoordinates = 2000;
            Rect area = new Rect();
            surfaceViewWithOverlay.GetDrawingRect(area);
            switch (orientation)
            {
                case 0:
                case 180:
                    height = cameraPreviewSize.Height;
                    width = cameraPreviewSize.Width;
                    break;
                case 90:
                case 270:
                    width = cameraPreviewSize.Height;
                    height = cameraPreviewSize.Width;
                    break;
            }

            camera.CancelAutoFocus();
            Android.Hardware.Camera.Parameters parameters = camera.GetParameters();
            // Set focus and metering area equal to the area of interest. This action is essential because by defaults camera
            // focuses on the center of the frame, while the area of interest in this sample application is at the top
            List<Android.Hardware.Camera.Area> focusAreas = new List<Android.Hardware.Camera.Area>();
            Rect areasRect;

            switch (orientation)
            {
                case 0:
                    areasRect = new Rect(
                        -halfCoordinates + area.Left * lengthCoordinates / width,
                        -halfCoordinates + area.Top * lengthCoordinates / height,
                        -halfCoordinates + lengthCoordinates * area.Right / width,
                        -halfCoordinates + lengthCoordinates * area.Bottom / height
                    );
                    break;
                case 180:
                    areasRect = new Rect(
                        halfCoordinates - area.Right * lengthCoordinates / width,
                        halfCoordinates - area.Bottom * lengthCoordinates / height,
                        halfCoordinates - lengthCoordinates * area.Left / width,
                        halfCoordinates - lengthCoordinates * area.Top / height
                    );
                    break;
                case 90:
                    areasRect = new Rect(
                        -halfCoordinates + area.Top * lengthCoordinates / height,
                        halfCoordinates - area.Right * lengthCoordinates / width,
                        -halfCoordinates + lengthCoordinates * area.Bottom / height,
                        halfCoordinates - lengthCoordinates * area.Left / width
                    );
                    break;
                case 270:
                    areasRect = new Rect(
                        halfCoordinates - area.Bottom * lengthCoordinates / height,
                        -halfCoordinates + area.Left * lengthCoordinates / width,
                        halfCoordinates - lengthCoordinates * area.Top / height,
                        -halfCoordinates + lengthCoordinates * area.Right / width
                    );
                    break;
                default:
                    throw new IllegalArgumentException();
            }

            focusAreas.Add(new Android.Hardware.Camera.Area(areasRect, 800));
            if (parameters.MaxNumFocusAreas >= focusAreas.Count)
            {
                parameters.FocusAreas = focusAreas;
            }
            if (parameters.MaxNumMeteringAreas >= focusAreas.Count)
            {
                parameters.MeteringAreas  =focusAreas;
            }

            parameters.FocusMode=mode;

            // Commit the camera parameters
            camera.SetParameters(parameters);
        }

        // Attach the camera to the surface holder, configure the camera and start preview
        public void setCameraPreviewDisplayAndStartPreview()
        {
            try
            {
                camera.SetPreviewDisplay(previewSurfaceHolder);
            }
            catch (Throwable t)
            {
                Log.Error(GetString(Resource.String.app_name), "Exception in setPreviewDisplay()", t);
            }
            configureCameraAndStartPreview(camera);
        }

        // Stop preview and release the camera
        private void stopPreviewAndReleaseCamera()
        {
            if (camera != null)
            {
                camera.SetPreviewCallbackWithBuffer(null);
                stopPreview();
                camera.Release();
                camera = null;
            }
        }

        // Stop preview if it is running
        private void stopPreview()
        {
            if (inPreview)
            {
                camera.StopPreview();
                inPreview = false;
            }
        }

        // Show error on startup if any
        private void showStartupError(string message)
        {
           var alert = new AlertDialog.Builder(this)
                .SetTitle("ABBYY RTR SDK")
                .SetMessage(message)
                .SetIcon(Android.Resource.Drawable.IcDialogAlert)
                .Show();
            alert.DismissEvent += alert_DismissEvent;
        }

        private void alert_DismissEvent(object sender, System.EventArgs e)
        {
            this.Finish();
        }

        // Load ABBYY RTR SDK engine and configure the text capture service
        private bool createTextCaptureService()
        {
            // Initialize the engine and text capture service
            try
            {
                engine = Engine.Load(this, licenseFileName);
                textCaptureService = engine.CreateTextCaptureService(textCaptureCallback);

                return true;
            }
            catch (Java.IO.IOException e)
            {
                //Troubleshooting for the developer

               Log.Error(GetString(Resource.String.app_name), "Error loading ABBYY RTR SDK:", e);
               showStartupError("Could not load some required resource files. Make sure to configure " +
                   "'assets' directory in your application and specify correct 'license file name'. See logcat for details.");
            }
            catch (Engine.LicenseException e)
            {
                // Troubleshooting for the developer
                Log.Error(GetString(Resource.String.app_name), "Error loading ABBYY RTR SDK:", e);
                showStartupError("License not valid. Make sure you have a valid license file in the " +
                    "'assets' directory and specify correct 'license file name' and 'application id'. See logcat for details.");
            }
            catch (Throwable e)
            {
                // Troubleshooting for the developer
                Log.Error(GetString(Resource.String.app_name), "Error loading ABBYY RTR SDK:", e);
                showStartupError("Unspecified error while loading the engine. See logcat for details.");
            }

            return false;
        }

        // Start recognition
        public void startRecognition()
        {
            // Do not switch off the screen while text capture service is running
            previewSurfaceHolder.SetKeepScreenOn(true);
            // Get area of interest (in coordinates of preview frames)
            Rect areaOfInterest = new Rect();
            surfaceViewWithOverlay.GetDrawingRect(areaOfInterest);
            // Clear error message
            errorTextView.Text = "";
            // Start the service
            textCaptureService.Start(cameraPreviewSize.Width, cameraPreviewSize.Height, orientation, areaOfInterest);
            // Change the text on the start button to 'Stop'
            startButton.Text = BUTTON_TEXT_STOP;
            startButton.Enabled = true;
        }

        // Stop recognition
        public void stopRecognition()
        {
            // Disable the 'Stop' button
            startButton.Enabled = false;

            // Stop the service asynchronously to make application more responsive. Stopping can take some time
            // waiting for all processing threads to stop

            Task.Run(() =>
            {
                textCaptureService.Stop();
                Thread.Sleep(1000);
                onPostExecute();

            });
            
        }

        void onPostExecute()
        {
            if (previewSurfaceHolder != null)
            {
                // Restore normal power saving behaviour
                previewSurfaceHolder.SetKeepScreenOn(false);
            }
            // Change the text on the stop button back to 'Start'
            startButton.Text = BUTTON_TEXT_START;
            startButton.Enabled = true;
        }

        // Clear recognition results
        void clearRecognitionResults()
        {
            stableResultHasBeenReached = false;
            surfaceViewWithOverlay.setLines(null, RecognitionServiceResultStabilityStatus.NotReady);
            surfaceViewWithOverlay.setFillBackground(false);
        }

        // Returns orientation of camera
        private int getCameraOrientation()
        {
            Display display = WindowManager.DefaultDisplay;
            int orientation = 0;
            switch (display.Rotation)
            {
                case SurfaceOrientation.Rotation0:
                    orientation = 0;
                    break;
                case SurfaceOrientation.Rotation90:
                    orientation = 90;
                    break;
                case SurfaceOrientation.Rotation180:
                    orientation = 180;
                    break;
                case SurfaceOrientation.Rotation270:
                    orientation = 270;
                    break;
            }
            for (int i = 0; i < Android.Hardware.Camera.NumberOfCameras; i++)
            {
                Android.Hardware.Camera.CameraInfo cameraInfo = new Android.Hardware.Camera.CameraInfo();
                Android.Hardware.Camera.GetCameraInfo(i, cameraInfo);
                if (cameraInfo.Facing == Android.Hardware.CameraFacing.Back)
                {
                    return (cameraInfo.Orientation - orientation + 360) % 360;
                }
            }
            // If Camera.open() succeed, this point of code never reached
            return -1;
        }

        private void configureCameraAndStartPreview(Android.Hardware.Camera camera)
        {
            // Setting camera parameters when preview is running can cause crashes on some android devices
            stopPreview();

            // Configure camera orientation. This is needed for both correct preview orientation
            // and recognition
            orientation = getCameraOrientation();
            camera.SetDisplayOrientation(orientation);

            // Configure camera parameters
            Android.Hardware.Camera.Parameters parameters = camera.GetParameters();

            // Select preview size. The preferred size for Text Capture scenario is 1080x720. In some scenarios you might
            // consider using higher resolution (small text, complex background) or lower resolution (better performance, less noise)
            cameraPreviewSize = null;
            foreach (Android.Hardware.Camera.Size size in parameters.SupportedPreviewSizes)
            {
                if (size.Height <= 720 || size.Width <= 720)
                {
                    if (cameraPreviewSize == null)
                    {
                        cameraPreviewSize = size;
                    }
                    else
                    {
                        int resultArea = cameraPreviewSize.Width * cameraPreviewSize.Height;
                        int newArea = size.Width * size.Height;
                        if (newArea > resultArea)
                        {
                            cameraPreviewSize = size;
                        }
                    }
                }
            }
            parameters.SetPreviewSize(cameraPreviewSize.Width, cameraPreviewSize.Height);

            // Zoom
            parameters.Zoom = cameraZoom;
            // Buffer format. The only currently supported format is NV21
            parameters.PreviewFormat = Android.Graphics.ImageFormatType.Nv21;
            // Default focus mode
            parameters.FocusMode = Android.Hardware.Camera.Parameters.FocusModeAuto;

            // Done
            camera.SetParameters(parameters);

            // The camera will fill the buffers with image data and notify us through the callback.
            // The buffers will be sent to camera on requests from recognition service (see implementation
            // of ITextCaptureService.Callback.onRequestLatestFrame above)
            camera.SetPreviewCallbackWithBuffer(cameraPreviewCallback);

            // Clear the previous recognition results if any
            clearRecognitionResults();

            // Width and height of the preview according to the current screen rotation
            int width = 0;
            int height = 0;
            switch (orientation)
            {
                case 0:
                case 180:
                    width = cameraPreviewSize.Width;
                    height = cameraPreviewSize.Height;
                    break;
                case 90:
                case 270:
                    width = cameraPreviewSize.Height;
                    height = cameraPreviewSize.Width;
                    break;
            }

            // Configure the view scale and area of interest (camera sees it as rotated 90 degrees, so
            // there's some confusion with what is width and what is height)
            surfaceViewWithOverlay.setScaleX(surfaceViewWithOverlay.Width, width);
            surfaceViewWithOverlay.setScaleY(surfaceViewWithOverlay.Height, height);
            // Area of interest
            int marginWidth = (areaOfInterestMargin_PercentOfWidth * width) / 100;
            int marginHeight = (areaOfInterestMargin_PercentOfHeight * height) / 100;
            surfaceViewWithOverlay.setAreaOfInterest(
                new Rect(marginWidth, marginHeight, width - marginWidth,
                    height - marginHeight));

            // Start preview
            camera.StartPreview();

            setCameraFocusMode(Android.Hardware.Camera.Parameters.FocusModeAuto);            
            autoFocus(finishCameraInitialisationAutoFocusCallback);

            inPreview = true;
        }

        // Initialize recognition language spinner in the UI with available languages
        ISharedPreferences preferences;
        string recognitionLanguageKey = "RecognitionLanguage";
        private void initializeRecognitionLanguageSpinner()
        {
            preferences = PreferenceManager.GetDefaultSharedPreferences(this);
            Spinner languageSpinner = (Spinner)FindViewById(Resource.Id.recognitionLanguageSpinner);

            //Make the collapsed spinner the size of the selected item
            ArrayAdapterCus adapter = new ArrayAdapterCus(ApplicationContext, Resource.Layout.Spinner);

            // Stored preference
            
            string selectedLanguage = preferences.GetString(recognitionLanguageKey, "English");

            // Fill the spinner with available languages selecting the previously chosen language
            int selectedIndex = -1;
            for (int i = 0; i < languages.Length; i++)
            {
                string name = languages[i].Name();
                adapter.Add(name);
                if (name.ToLower().Equals(selectedLanguage.ToLower()))
                {
                    selectedIndex = i;
                }
            }
            if (selectedIndex == -1)
            {
                adapter.Insert(selectedLanguage, 0);
                selectedIndex = 0;
            }

            languageSpinner.Adapter = adapter;

            if (selectedIndex != -1)
            {
                languageSpinner.SetSelection(selectedIndex);
            }

            languageSpinner.ItemSelected += LanguageSpinner_ItemSelected;
        }

        private void LanguageSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string recognitionLanguage = (string)e.Parent.GetItemAtPosition(e.Position);
            if (textCaptureService != null)
            {
                // Reconfigure the recognition service each time a new language is selected
                // This is also called when the spinner is first shown
                textCaptureService.SetRecognitionLanguage(Language.ValueOf(recognitionLanguage));
                clearRecognitionResults();
            }
            if (!preferences.GetString(recognitionLanguageKey, "").ToLower().Equals(recognitionLanguage.ToLower()))
            {
                // Store the selection in preferences
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutString(recognitionLanguageKey, recognitionLanguage);
                editor.Commit();
            }
        }

        // The 'Start' and 'Stop' button
        public void onStartButtonClick(View view)
        {
            if (startButton.Text.Equals(BUTTON_TEXT_STOP))
            {
                stopRecognition();
            }
            else
            {
                clearRecognitionResults();
                startButton.Enabled = false;
                startButton.Text = BUTTON_TEXT_STARTING;
                if (!isContinuousVideoFocusModeEnabled(camera))
                {
                    autoFocus(startRecognitionCameraAutoFocusCallback);
                }
                else
                {
                    startRecognition();
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Reinitialize the camera, restart the preview and recognition if required
            startButton.Enabled = false;
            clearRecognitionResults();
            startRecognitionWhenReady = startRecognitionOnAppStart;
            camera = Android.Hardware.Camera.Open();
            if (previewSurfaceHolder != null)
            {
                setCameraPreviewDisplayAndStartPreview();
            }
        }

        protected override void OnPause()
        {
            // Clear all pending actions
            handler.RemoveCallbacksAndMessages(null);
            // Stop the text capture service
            if (textCaptureService != null)
            {
                textCaptureService.Stop();
            }
            startButton.Text = BUTTON_TEXT_START;
            // Clear recognition results
            clearRecognitionResults();
            stopPreviewAndReleaseCamera();
            base.OnPause();
        }

        // Start autofocus (used when continuous autofocus is disabled)
        public void autoFocus(Android.Hardware.Camera.IAutoFocusCallback callback)
        {
            if (camera != null)
            {
                try
                {
                    setCameraFocusMode(Android.Hardware.Camera.Parameters.FocusModeAuto);
                    camera.AutoFocus(callback);
                }
                catch (Exception e)
                {
                    Log.Error(GetString(Resource.String.app_name), "Error: " + e.Message);
                }
            }
        }

        public void onAutoFocusFinished(bool success, Android.Hardware.Camera camera)
        {
            if (isContinuousVideoFocusModeEnabled(camera))
            {
                setCameraFocusMode(Android.Hardware.Camera.Parameters.FocusModeContinuousVideo);
            }
            else
            {
                if (!success)
                {
                    autoFocus(simpleCameraAutoFocusCallback);
                }
            }
        }
    }
    }


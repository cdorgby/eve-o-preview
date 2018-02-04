using System;
using System.Threading;
using System.Windows.Forms;
using EveOPreview.Configuration;
using EveOPreview.Mediator;
using EveOPreview.UI;
using EveOPreview.WindowManager;

namespace EveOPreview
{
	static class Program
	{
		private static string MutexName = "EVE-O Preview Single Instance Mutex";

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		static void Main()
		{
			// The very usual Mutex-based single-instance screening
			// 'token' variable is used to store reference to the instance Mutex
			// during the app lifetime
			object token = Program.GetInstanceToken();

			// If it was not possible to aquire the app token then another app instance is already running
			// Nothing to do here
			if (token == null)
			{
				return;
			}

			ExceptionHandler handler = new ExceptionHandler();
			handler.SetupExceptionHandlers();

			IApplicationController controller = Program.InitializeApplicationController();

			Program.InitializeWinForms();
			controller.Run<MainPresenter>();
		}

		private static object GetInstanceToken()
		{
			// The code might look overcomplicated here for a single Mutex operation
			// Yet we had already experienced a Windows-level issue
			// where .NET finalizer theread was literally paralyzed by
			// a failed Mutex operation. That did lead to weird OutOfMemory
			// exceptions later
			try
			{
				Mutex mutex = Mutex.OpenExisting(Program.MutexName);
				// if that didn't fail then anotherinstance is already running
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
			catch (Exception)
			{
				Mutex token = new Mutex(true, Program.MutexName, out var result);
				return result ? token : null;
			}
		}

		private static void InitializeWinForms()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
		}

		private static IApplicationController InitializeApplicationController()
		{
			IIocContainer container = new LightInjectContainer();

			// Singleton registration is used for services
			// Low-level services
			container.Register<IMediator>();
			container.Register<IWindowManager>();

			// Configuration services
			container.Register<IConfigurationStorage>();
			container.Register<IAppConfig>();
			container.Register<IThumbnailConfiguration>();

			// Application services
			container.Register<IThumbnailManager>();
			container.Register<IThumbnailViewFactory>();
			container.Register<IThumbnailDescriptionViewFactory>();

			IApplicationController controller = new ApplicationController(container);

			// UI classes
			controller.RegisterView<IMainView, MainForm>()
				.RegisterView<IThumbnailView, ThumbnailView>()
				.RegisterView<IThumbnailDescriptionView, ThumbnailDescriptionView>()
				.RegisterInstance(new ApplicationContext());

			return controller;
		}
	}
}
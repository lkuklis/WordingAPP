using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Wording.Core;
using Wording.Desktop.Notifications;
using Wording.Shell;

namespace Wording.Desktop;

public partial class App : Application
{
    WordingHost _host = null!;
    INotifier _notifier = null!;
    MainWindow _okno = null!;
    DispatcherTimer _timer = null!;

    NativeMenuItem _ocenZnam = null!;
    NativeMenuItem _ocenTrudne = null!;
    NativeMenuItem _ocenNieZnam = null!;

    /// <summary>Ostatnio pokazane slowko - to jego dotycza oceny z paska menu.</summary>
    Word? _ostatnioPokazane;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Aplikacja zyje w pasku menu - zamkniecie okna nie moze konczyc procesu.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _host = WordingHost.Create();
            _notifier = OperatingSystem.IsMacOS() ? new MacNotifier() : new NullNotifier();

            _okno = new MainWindow(_host.Manager);
            desktop.MainWindow = _okno;
            _okno.Show();

            ZbudujIkoneWPasku();
            UruchomZegar();
        }

        base.OnFrameworkInitializationCompleted();
    }

    void ZbudujIkoneWPasku()
    {
        _ocenZnam = new NativeMenuItem("I know it");
        _ocenZnam.Click += (_, _) => Ocen(Core.Learning.ReviewGrade.Good);

        _ocenTrudne = new NativeMenuItem("Hard");
        _ocenTrudne.Click += (_, _) => Ocen(Core.Learning.ReviewGrade.Hard);

        _ocenNieZnam = new NativeMenuItem("Don't know");
        _ocenNieZnam.Click += (_, _) => Ocen(Core.Learning.ReviewGrade.Again);

        var pokazOkno = new NativeMenuItem("Show window");
        pokazOkno.Click += (_, _) => _okno.PokazIAktywuj();

        var zakoncz = new NativeMenuItem("Exit");
        zakoncz.Click += (_, _) =>
        {
            _timer.Stop();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };

        var menu = new NativeMenu
        {
            Items =
            {
                _ocenZnam,
                _ocenTrudne,
                _ocenNieZnam,
                new NativeMenuItemSeparator(),
                pokazOkno,
                zakoncz,
            },
        };

        UstawDostepnoscOcen();

        var ikona = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Wording.Desktop/Assets/Icon1.ico"))),
            ToolTipText = "Wording",
            Menu = menu,
            IsVisible = true,
        };

        ikona.Clicked += (_, _) => _okno.PokazIAktywuj();

        // Rejestracja przez wlasciwosc dolaczona - inaczej ikona pada ofiara GC.
        TrayIcon.SetIcons(this, [ikona]);
    }

    void UruchomZegar()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_host.Settings.ChangeTimeSeconds),
        };

        _timer.Tick += (_, _) => PokazKolejneSlowko();
        _timer.Start();
    }

    void PokazKolejneSlowko()
    {
        var slowo = _host.Manager.NextWordToShow();

        if (slowo is null)
        {
            return;
        }

        _ostatnioPokazane = slowo;
        UstawDostepnoscOcen();

        _notifier.Show(slowo.Original, slowo.Translation);
    }

    void Ocen(Core.Learning.ReviewGrade ocena)
    {
        if (_ostatnioPokazane is null)
        {
            return;
        }

        _host.Manager.Grade(_ostatnioPokazane.Id, ocena);
        _ostatnioPokazane = null;

        UstawDostepnoscOcen();
        _okno.Odswiez();
    }

    void UstawDostepnoscOcen()
    {
        var mozna = _ostatnioPokazane is not null;
        var opis = _ostatnioPokazane is null ? string.Empty : $" — {_ostatnioPokazane.Original}";

        _ocenZnam.IsEnabled = mozna;
        _ocenTrudne.IsEnabled = mozna;
        _ocenNieZnam.IsEnabled = mozna;

        _ocenZnam.Header = "I know it" + opis;
        _ocenTrudne.Header = "Hard" + opis;
        _ocenNieZnam.Header = "Don't know" + opis;
    }
}

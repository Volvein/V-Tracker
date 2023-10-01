using BackgroundService;
using Topshelf;

var rc = HostFactory.Run(x =>
{
    x.Service<ImageService>(s =>
    {
        s.ConstructUsing(name => new ImageService());
        s.WhenStarted(tc => tc.starttimer());
        s.WhenStopped(tc => tc.stoptimer());
    });
    x.RunAsLocalSystem();
    x.SetServiceName("ImageService");
    x.SetDisplayName("ImageService");
    x.SetDescription("This is a service to save images");
});

var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
Environment.ExitCode = exitCode;

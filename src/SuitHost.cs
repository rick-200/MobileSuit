﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlasticMetal.MobileSuit.Core;
using PlasticMetal.MobileSuit.Core.Services;

namespace PlasticMetal.MobileSuit;

/// <summary>
///     A entity, which serves the shell functions of a mobile-suit program.
/// </summary>
internal class SuitHost : IMobileSuitHost
{
    private readonly ISuitExceptionHandler _exceptionHandler;
    private readonly AsyncServiceScope _rootScope;
    private readonly IReadOnlyList<ISuitMiddleware> _suitApp;
    private IHostApplicationLifetime _lifetime;
    private TaskCompletionSource _startUp;
    private readonly TaskRecorder _cancellationTasks;
    private Task? _hostTask;

    public SuitHost(IServiceProvider services, 
        IReadOnlyList<ISuitMiddleware> middleware,
        TaskCompletionSource startUp,
        TaskRecorder cancellationTasks)
    {
        Services = services;
        _suitApp = middleware;
        _startUp = startUp;
        _cancellationTasks = cancellationTasks;
        _lifetime = Services.GetRequiredService<IHostApplicationLifetime>();
        _exceptionHandler = Services.GetRequiredService<ISuitExceptionHandler>();
        _rootScope = Services.CreateAsyncScope();
        Logger = Services.GetRequiredService<ILogger<SuitHost>>();

    }

    /// <inheritdoc />
    public ILogger Logger { get; }

    public IServiceProvider Services { get; }

    public void Dispose()
    {
        _rootScope.Dispose();
    }


    public async Task StartAsync(CancellationToken cancellationToken = new())
    {
        if (_hostTask is not null) return;
        Console.CancelKeyPress += StartTimeCancelKeyPress;

        var requestStack = new Stack<SuitRequestDelegate>();
        requestStack.Push(_ => Task.CompletedTask);


        foreach (var middleware in _suitApp.Reverse())
        {
            var next = requestStack.Peek();
            requestStack.Push(async c => await middleware.InvokeAsync(c, next));
        }

        var handleRequest = requestStack.Peek();
        var appInfo = _rootScope.ServiceProvider.GetRequiredService<ISuitAppInfo>();
        Console.CancelKeyPress -= StartTimeCancelKeyPress;
        if (cancellationToken.IsCancellationRequested) return;
        if (appInfo.StartArgs.Length > 0)
        {
            var requestScope = Services.CreateScope();
            var context = new SuitContext(requestScope)
            {
                Status = RequestStatus.NotHandled,
                Request = appInfo.StartArgs
            };
            var cancelKeyHandler=CreateCancelKeyHandler(context);
            Console.CancelKeyPress += cancelKeyHandler;
            try
            {
                await handleRequest(context);
            }
            catch (Exception ex)
            {
                context.Exception = ex;
                context.Status = RequestStatus.Faulted;
                await _exceptionHandler.InvokeAsync(context);
            }
            Console.CancelKeyPress -= cancelKeyHandler;
        }

        _startUp.SetResult();
        _hostTask = HandleRequest(handleRequest);
    }

    public async Task StopAsync(CancellationToken cancellationToken = new())
    {
        if (_hostTask is null) return;
        //await _hostTask;
        await _rootScope.DisposeAsync();
        _hostTask = null;
    }

    private async Task HandleRequest(SuitRequestDelegate requestHandler)
    {
        for (; ; )
        {
            var requestScope = Services.CreateScope();
            var context = new SuitContext(requestScope);
            var cancelKeyHandler = CreateCancelKeyHandler(context);
            Console.CancelKeyPress += cancelKeyHandler;
            try
            {
                await requestHandler(context);
            }
            catch (Exception ex)
            {
                context.Exception = ex;
                context.Status = RequestStatus.Faulted;
                await _exceptionHandler.InvokeAsync(context);
                continue;
            }
            Console.CancelKeyPress -= cancelKeyHandler;
            if (context.Status == RequestStatus.OnExit || 
                context.Status == RequestStatus.NoRequest &&
                context.CancellationToken.IsCancellationRequested) break;
        }
        _lifetime.StopApplication();
    }

    private void StartTimeCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
    }

    private static ConsoleCancelEventHandler CreateCancelKeyHandler(SuitContext context)
    {
        return (sender, e) =>
        {
            e.Cancel = true;
            context.CancellationToken.Cancel();
        };
    }


    public async ValueTask DisposeAsync()
    {
        await _rootScope.DisposeAsync();
    }
}
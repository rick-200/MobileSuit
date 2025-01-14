﻿using System.Threading.Tasks;

namespace PlasticMetal.MobileSuit.Core.Services;

/// <summary>
///     The handler
/// </summary>
public interface ISuitExceptionHandler
{
    /// <summary>
    /// Define whether Exception will be thrown when faulted.
    /// </summary>
    public static bool ThrowIfFaulted { get; set; }
    /// <summary>
    ///     To invoke the middleware
    /// </summary>
    /// <param name="context">Context of the request.</param>
    /// <returns></returns>
    public Task InvokeAsync(SuitContext context);
}

internal class SuitExceptionHandler : ISuitExceptionHandler
{
    public SuitExceptionHandler(IHistoryService history, IIOHub io)
    {
        History = history;
        IO = io;
    }

    public IHistoryService History { get; }
    public IIOHub IO { get; }

    public async Task InvokeAsync(SuitContext context)
    {
        if (context.Exception is null)
        {
            History.Status = RequestStatus.Faulted;
            History.Response = Lang.ApplicationError;
        }
        else
        {
            if (ISuitExceptionHandler.ThrowIfFaulted)
            {
                throw context.Exception;
            }
            History.Status = RequestStatus.Faulted;
            History.Response = context.Exception.Message;
            await IO.WriteLineAsync(context.Exception.Message, OutputType.Error);
        }
    }
}
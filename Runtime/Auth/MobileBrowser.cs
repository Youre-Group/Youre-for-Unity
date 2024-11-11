/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */


using System.Threading;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Browser;
using UnityEngine;
using YourePlugin;

namespace Auth
{
    public abstract class MobileBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _task;
        protected abstract void Launch(string url);
        public virtual void Dismiss() { }

        public void OnAuthReply(string value)
        {
            Youre.LogDebug("MobileBrowser.OnAuthReply: " + value);
            if (_task == null)
            {
                Youre.LogDebug("Task was not invoked before");
            }
            _task.SetResult(new BrowserResult()
            {
                Response = value
            });
        }

        public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = new CancellationToken())
        {
            _task = new TaskCompletionSource<BrowserResult>();
            Launch(options.StartUrl);
            return _task.Task;
        }
    }
}

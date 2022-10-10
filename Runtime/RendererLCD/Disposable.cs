using System;

namespace Illumetry.Unity {
    public sealed class Disposable : IDisposable {
        public Disposable(Action onDispose) {
            OnDispose = onDispose;
        }
        public Action OnDispose { get; }
        public void Dispose() => OnDispose?.Invoke();
    }
}
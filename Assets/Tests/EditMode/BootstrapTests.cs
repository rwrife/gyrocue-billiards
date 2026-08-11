using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class BootstrapTests
    {
        [Test]
        public void GameBootstrap_DefaultsToDualPhoneCueEnabled()
        {
            var root = new GameObject("bootstrap-test-root");

            try
            {
                var bootstrap = root.AddComponent<GameBootstrap>();
                Assert.That(bootstrap.UseDualPhoneCue, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

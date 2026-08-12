using System.IO;
using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class TableLayoutConstantsTests
    {
        [Test]
        public void CalculateOrthoSize_FitsTableHeightAtPortraitAspect()
        {
            var size = TableLayoutConstants.CalculateOrthoSize(TableLayoutConstants.TargetPhoneAspect);
            var minimumHeightFit = (TableLayoutConstants.TableHeightWorldUnits * 0.5f) + TableLayoutConstants.CameraPadding;

            Assert.That(size, Is.GreaterThanOrEqualTo(minimumHeightFit));
        }

        [Test]
        public void CalculateOrthoSize_GrowsForUltraNarrowAspect()
        {
            var portraitSize = TableLayoutConstants.CalculateOrthoSize(TableLayoutConstants.TargetPhoneAspect);
            var narrowSize = TableLayoutConstants.CalculateOrthoSize(9f / 21f);

            Assert.That(narrowSize, Is.GreaterThan(portraitSize));
        }

        [Test]
        public void MainTableScene_ContainsExpectedPlaceholderObjects()
        {
            var scenePath = Path.Combine(Application.dataPath, "Scenes", "MainTable.unity");
            Assert.That(File.Exists(scenePath), Is.True, $"Scene file was not found at: {scenePath}");

            var sceneText = File.ReadAllText(scenePath);
            StringAssert.Contains("m_Name: Main Camera", sceneText);
            StringAssert.Contains("m_Name: TableBoundsPlaceholder", sceneText);

            var cushionCount = CountOccurrences(sceneText, "Cushion");
            var pocketCount = CountOccurrences(sceneText, "Pocket");

            Assert.That(cushionCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(pocketCount, Is.GreaterThanOrEqualTo(6));
        }

        private static int CountOccurrences(string text, string token)
        {
            var count = 0;
            var index = 0;

            while ((index = text.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }
    }
}

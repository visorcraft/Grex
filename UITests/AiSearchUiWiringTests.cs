using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Grex.UITests
{
    [Collection("UI SettingsOverride collection")]
    public class AiSearchUiWiringTests
    {
        [UITestMethod]
        public void SearchTabContentXaml_ShouldContainAiButtonChatPanelAndCompactSearchButton()
        {
            // Arrange
            var xamlPath = Path.Combine(GetRepositoryRoot(), "Controls", "SearchTabContent.xaml");
            var xaml = File.ReadAllText(xamlPath);

            // Assert
            xaml.Should().Contain("x:Name=\"AppBarSearchButton\"");
            xaml.Should().Contain("AppBarSearchButton\" x:Uid=\"AppBarSearchButton\" Label=\"Search\" LabelPosition=\"Collapsed\"");

            xaml.Should().Contain("x:Name=\"AppBarAiButton\"");
            xaml.Should().Contain("x:Uid=\"AppBarAiButton\" Label=\"AI\"");
            xaml.Should().Contain("Click=\"AppBarAiButton_Click\"");
            xaml.Should().Contain("Glyph=\"&#x1F916;\"");
            xaml.Should().Contain("FontFamily=\"Segoe UI Emoji\"");

            xaml.Should().Contain("x:Name=\"AiChatPanel\"");
            xaml.Should().Contain("x:Name=\"AiChatInputTextBox\"");
            xaml.Should().Contain("x:Name=\"AiSendButton\"");
            xaml.Should().Contain("Click=\"AiSendButton_Click\"");
            xaml.Should().Contain("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"");

            var aiPanelOpening = Regex.Match(xaml, "<Border\\s+x:Name=\"AiChatPanel\"[\\s\\S]*?>");
            aiPanelOpening.Success.Should().BeTrue();
            aiPanelOpening.Value.Should().Contain("Grid.Row=\"4\"");
            aiPanelOpening.Value.Should().Contain("VerticalAlignment=\"Stretch\"");
            aiPanelOpening.Value.Should().NotContain("Grid.RowSpan=");
        }

        [UITestMethod]
        public void SettingsViewXaml_ShouldContainAiEndpointApiKeyAndModelControls()
        {
            // Arrange
            var xamlPath = Path.Combine(GetRepositoryRoot(), "Controls", "SettingsView.xaml");
            var xaml = File.ReadAllText(xamlPath);

            // Assert
            xaml.Should().Contain("x:Name=\"AiSearchEndpointTextBox\"");
            xaml.Should().Contain("TextChanged=\"AiSearchEndpointTextBox_TextChanged\"");

            xaml.Should().Contain("x:Name=\"AiSearchApiKeyPasswordBox\"");
            xaml.Should().Contain("PasswordChanged=\"AiSearchApiKeyPasswordBox_PasswordChanged\"");

            xaml.Should().Contain("x:Name=\"AiSearchModelTextBox\"");
            xaml.Should().Contain("TextChanged=\"AiSearchModelTextBox_TextChanged\"");

            xaml.Should().Contain("x:Name=\"TestAiEndpointButton\"");
            xaml.Should().Contain("Click=\"TestAiEndpointButton_Click\"");
        }

        [UITestMethod]
        public void SearchTabContentCodeBehind_ShouldCollapseFilterOptionsWhenAiModeStarts()
        {
            // Arrange
            var codeBehindPath = Path.Combine(GetRepositoryRoot(), "Controls", "SearchTabContent.xaml.cs");
            var codeBehind = File.ReadAllText(codeBehindPath);

            // Assert
            codeBehind.Should().Contain("private void CollapseFilterOptionsPane()");
            Regex.Matches(codeBehind, "CollapseFilterOptionsPane\\(\\);").Count.Should().BeGreaterThanOrEqualTo(2);
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "grex.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located from test context.");
        }
    }
}

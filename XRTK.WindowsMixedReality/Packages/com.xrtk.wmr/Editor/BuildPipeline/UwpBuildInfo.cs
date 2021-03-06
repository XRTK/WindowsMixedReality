// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Services;

namespace XRTK.Editor.BuildPipeline
{
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    public class UwpBuildInfo : BuildInfo
    {
        /// <inheritdoc />
        public override BuildTarget BuildTarget => BuildTarget.WSAPlayer;

        private Version version;

        /// <inheritdoc />
        public override Version Version
        {
            get
            {
                if (version == null)
                {
                    version = PlayerSettings.WSA.packageVersion;
                }

                return version;
            }
            set => version = value;
        }

        private string solutionName;

        /// <summary>
        /// The name of the Visual Studio .sln file generated.
        /// </summary>
        public string SolutionName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(solutionName))
                {
                    solutionName = $"{PlayerSettings.productName}\\{PlayerSettings.productName}.sln";
                }

                return solutionName;
            }
        }

        /// <summary>
        /// Build the appx bundle after building Unity Player?
        /// </summary>
        public bool BuildAppx { get; set; }

        /// <summary>
        /// Force rebuilding the appx bundle?
        /// </summary>
        public bool RebuildAppx { get; set; }

        public string UwpSdk => EditorUserBuildSettings.wsaUWPSDK;

        public string MinSdk => EditorUserBuildSettings.wsaMinUWPSDK;

        private PlayerSettings.WSATargetFamily[] buildTargetFamilies;

        public PlayerSettings.WSATargetFamily[] BuildTargetFamilies => buildTargetFamilies ?? (buildTargetFamilies = GetFamilies());

        private static PlayerSettings.WSATargetFamily[] GetFamilies()
        {
            var values = (PlayerSettings.WSATargetFamily[])Enum.GetValues(typeof(PlayerSettings.WSATargetFamily));
            return values.Where(PlayerSettings.WSA.GetTargetDeviceFamily).ToArray();
        }

        /// <inheritdoc />
        public override void OnPostProcessBuild(BuildReport buildReport)
        {
            if (!MixedRealityToolkit.ActivePlatforms.Contains(BuildPlatform) ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget)
            {
                return;
            }

            if (buildReport.summary.result == BuildResult.Failed)
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog($"{PlayerSettings.productName} Build {buildReport.summary.result}!", "See console for details", "OK");
                }
            }
            else
            {
                if (BuildAppx ||
                    !Application.isBatchMode &&
                    !EditorUtility.DisplayDialog(PlayerSettings.productName, "Build Complete", "OK", "Build AppX"))
                {
                    UwpAppxBuildTools.BuildAppx(this);
                }
            }
        }

        /// <inheritdoc />
        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();

            string[] arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-buildAppx":
                        BuildAppx = true;
                        break;
                    case "-rebuildAppx":
                        RebuildAppx = true;
                        break;
                }
            }
        }
    }
}

// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class Fault : ModuleRules
{
	public Fault(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[] {
			"Core",
			"CoreUObject",
			"Engine",
			"InputCore",
			"EnhancedInput",
			"AIModule",
			"StateTreeModule",
			"GameplayStateTreeModule",
			"UMG",
			"Slate"
		});

		PrivateDependencyModuleNames.AddRange(new string[] { });

		PublicIncludePaths.AddRange(new string[] {
			"Fault",
			"Fault/Variant_Platforming",
			"Fault/Variant_Platforming/Animation",
			"Fault/Variant_Combat",
			"Fault/Variant_Combat/AI",
			"Fault/Variant_Combat/Animation",
			"Fault/Variant_Combat/Gameplay",
			"Fault/Variant_Combat/Interfaces",
			"Fault/Variant_Combat/UI",
			"Fault/Variant_SideScrolling",
			"Fault/Variant_SideScrolling/AI",
			"Fault/Variant_SideScrolling/Gameplay",
			"Fault/Variant_SideScrolling/Interfaces",
			"Fault/Variant_SideScrolling/UI"
		});

		// Uncomment if you are using Slate UI
		// PrivateDependencyModuleNames.AddRange(new string[] { "Slate", "SlateCore" });

		// Uncomment if you are using online features
		// PrivateDependencyModuleNames.Add("OnlineSubsystem");

		// To include OnlineSubsystemSteam, add it to the plugins section in your uproject file with the Enabled attribute set to true
	}
}

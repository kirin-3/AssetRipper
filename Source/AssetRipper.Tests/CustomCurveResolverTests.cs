using AssetRipper.Checksum;
using AssetRipper.Primitives;
using AssetRipper.Processing.AnimationClips;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Extensions.Enums.AnimationClip.GenericBinding;

namespace AssetRipper.Tests;

public class CustomCurveResolverTests
{
	[TestCase(15u, 0u)]
	[TestCase(31u, 1u)]
	[TestCase(47u, 2u)]
	[TestCase(63u, 3u)]
	[TestCase(79u, 4u)]
	public void ConstraintIndexUsesBitsAboveTheField(uint attribute, uint expected)
	{
		Assert.That(CustomCurveResolver.ConstraintIndex(attribute), Is.EqualTo(expected));
	}

	[TestCase(BindingCustomType.PositionConstraint, 11u, "m_Sources", "sourceTransform")]
	[TestCase(BindingCustomType.PositionConstraint, 12u, "m_Sources", "weight")]
	[TestCase(BindingCustomType.RotationConstraint, 11u, "m_Sources", "sourceTransform")]
	[TestCase(BindingCustomType.RotationConstraint, 12u, "m_Sources", "weight")]
	[TestCase(BindingCustomType.ScaleConstraint, 11u, "m_Sources", "sourceTransform")]
	[TestCase(BindingCustomType.ScaleConstraint, 12u, "m_Sources", "weight")]
	[TestCase(BindingCustomType.AimConstraint, 6u, "m_Sources", "sourceTransform")]
	[TestCase(BindingCustomType.AimConstraint, 7u, "m_Sources", "weight")]
	[TestCase(BindingCustomType.ParentConstraint, 8u, "m_TranslationOffsets", "x")]
	[TestCase(BindingCustomType.ParentConstraint, 9u, "m_TranslationOffsets", "y")]
	[TestCase(BindingCustomType.ParentConstraint, 10u, "m_TranslationOffsets", "z")]
	[TestCase(BindingCustomType.ParentConstraint, 11u, "m_RotationOffsets", "x")]
	[TestCase(BindingCustomType.ParentConstraint, 12u, "m_RotationOffsets", "y")]
	[TestCase(BindingCustomType.ParentConstraint, 13u, "m_RotationOffsets", "z")]
	[TestCase(BindingCustomType.ParentConstraint, 14u, "m_Sources", "sourceTransform")]
	[TestCase(BindingCustomType.ParentConstraint, 15u, "m_Sources", "weight")]
	[TestCase(BindingCustomType.LookAtConstraint, 3u, "m_Sources", "sourceTransform")]
	[TestCase(BindingCustomType.LookAtConstraint, 4u, "m_Sources", "weight")]
	public void ConstraintArraysKeepEachEntry(BindingCustomType type, uint field, string array, string member)
	{
		CustomCurveResolver resolver = CreateResolver();
		foreach (uint index in new uint[] { 0, 1, 2, 3, 4, 16 })
		{
			string name = resolver.ToAttributeName(type, (index << 4) | field, "");
			Assert.That(name, Is.EqualTo($"{array}.Array.data[{index}].{member}"));
		}
	}

	[TestCase(BindingCustomType.BlendShape, "blendShape.")]
	[TestCase(BindingCustomType.RendererMaterial, "material.")]
	public void UnknownPathKeepsEachBindingsOwnAttribute(BindingCustomType type, string prefix)
	{
		CustomCurveResolver resolver = CreateResolver();
		string path = Crc32Algorithm.ReverseAscii(0x7833C047, "path_0x7833C047_");
		const uint FirstAttribute = 0x4DAF8B71;
		const uint SecondAttribute = 0x5DAF8B71;
		string first = resolver.ToAttributeName(type, FirstAttribute, path);
		string second = resolver.ToAttributeName(type, SecondAttribute, path);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(first, Is.Not.EqualTo(second));
			Assert.That(first, Does.StartWith(prefix + "path_0x4DAF8B71_"));
			Assert.That(second, Does.StartWith(prefix + "path_0x5DAF8B71_"));
			Assert.That(Crc32Algorithm.HashUTF8(first[prefix.Length..]), Is.EqualTo(FirstAttribute));
			Assert.That(Crc32Algorithm.HashUTF8(second[prefix.Length..]), Is.EqualTo(SecondAttribute));
			Assert.That(first, Is.EqualTo(CustomCurveResolver.UnknownName(prefix, FirstAttribute)));
			Assert.That(second, Is.EqualTo(CustomCurveResolver.UnknownName(prefix, SecondAttribute)));
		}
	}

	private static CustomCurveResolver CreateResolver() =>
		new(AssetCreator.CreateAnimationClip(new UnityVersion(2022, 3, 22)));
}

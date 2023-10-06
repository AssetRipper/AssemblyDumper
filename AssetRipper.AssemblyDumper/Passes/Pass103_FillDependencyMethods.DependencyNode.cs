using AssetRipper.Assets.Generics;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetRipper.AssemblyDumper.Passes;

public static partial class Pass103_FillDependencyMethods
{
	private abstract partial class DependencyNode
	{
		public DependencyNode(DependencyNode? parent = null)
		{
			Parent = parent;
		}

		public abstract string PathContent { get; }
		public DependencyNode? Parent { get; }

		public string FullPath
		{
			get
			{
				if (Parent is null)
				{
					return PathContent;
				}

				StringBuilder sb = new();
				DependencyNode? current = this;
				while (current != null)
				{
					string content = current.PathContent;
					for (int i = content.Length - 1; i >= 0; i--)
					{
						sb.Append(content[i]);
					}
					if (current is FieldDependencyNode)
					{
						sb.Append('.');
					}
					current = current.Parent;
				}
				if (sb.Length > 0 && sb[^1] == '.')
				{
					sb.Remove(sb.Length - 1, 1);
				}

				ReverseCharacterOrder(sb);

				return sb.ToString();
			}
		}
		private static void ReverseCharacterOrder(StringBuilder sb)
		{
			int midpoint = sb.Length / 2;
			int lastIndex = sb.Length - 1;
			for (int i = 0; i < midpoint; i++)
			{
				char first = sb[i];
				char second = sb[lastIndex - i];
				sb[i] = second;
				sb[lastIndex - i] = first;
			}
		}
		/// <summary>
		/// The name of the field that stores the state of this node, if it has one.
		/// </summary>
		public string StateFieldName => $"{InvalidMethodCharacters().Replace(FullPath, "_")}_state{StateFieldTypeCharacter}";

		public virtual char StateFieldTypeCharacter => 'U';

		public abstract TypeSignature TypeSignature { get; }
		public virtual IEnumerable<DependencyNode> Children => Enumerable.Empty<DependencyNode>();
		public virtual bool AnyPPtrs => Children.Any(c => c.AnyPPtrs);
		public abstract void Apply(DependencyMethodContext context, ParentContext parentContext);

		public static DependencyNode Create(TypeSignature type, DependencyNode parent)
		{
			switch (type)
			{
				case CorLibTypeSignature or SzArrayTypeSignature:
					return new PrimitiveDependencyNode(type, parent);
				case TypeDefOrRefSignature typeDefOrRefSignature:
					if (typeDefOrRefSignature.Type is TypeDefinition typeDefinition)
					{
						GeneratedClassInstance instance = SharedState.Instance.TypesToInstances[typeDefinition];
						return instance.Group.IsPPtr
							? new PPtrDependencyNode(instance, parent)
							: new TypeDependencyNode(instance, parent);
					}
					else
					{
						return new PrimitiveDependencyNode(type, parent);//Utf8String
					}
				case GenericInstanceTypeSignature genericInstanceTypeSignature:
					return (genericInstanceTypeSignature.GenericType.Name?.ToString()) switch
					{
						$"{nameof(AssetDictionary<int, int>)}`2" => new DictionaryDependencyNode(genericInstanceTypeSignature, parent),
						$"{nameof(AssetList<int>)}`1" => new ArrayDependencyNode(genericInstanceTypeSignature, parent),
						$"{nameof(AssetPair<int, int>)}`2" => new PairDependencyNode(genericInstanceTypeSignature, parent),
						_ => throw new NotSupportedException(),
					};
				default:
					throw new NotImplementedException();
			}
		}

		[GeneratedRegex(@"[\[\].]")]
		private static partial Regex InvalidMethodCharacters();
	}
}

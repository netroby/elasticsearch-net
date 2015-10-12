using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.Serialization;
using Newtonsoft.Json;

namespace Nest
{
	[JsonConverter(typeof(IndicesJsonConverter))]
	public class Indices : Union<Indices.AllIndicesMarker, Indices.ManyIndices>, IUrlParameter
	{
		public class AllIndicesMarker { internal AllIndicesMarker() { } }
		public static Indices All { get; } = new Indices(new AllIndicesMarker());
		public static Indices AllIndices { get; } = new Indices(new AllIndicesMarker());
		public class ManyIndices
		{
			private readonly List<IndexName> _indices = new List<IndexName>();
			public IReadOnlyList<IndexName> Indices => _indices;
			internal ManyIndices(IEnumerable<IndexName> indices) { this._indices.AddRange(indices); }

			public ManyIndices And<T>()
			{
				this._indices.Add(typeof(T));
				return this;
			}
		}

		internal Indices(Indices.AllIndicesMarker all) : base(all) { }
		internal Indices(Indices.ManyIndices indices) : base(indices) { }
		internal Indices(IEnumerable<IndexName> indices) : base(new ManyIndices(indices)) { }

		public static IndexName Index(IndexName index) => index;
		public static IndexName Index<T>() => typeof(T);
		public static ManyIndices Index(IEnumerable<IndexName> indices) => new ManyIndices(indices);
		public static ManyIndices Index(params IndexName[] indices) => new ManyIndices(indices);

		public static Indices Parse(string indicesString)
		{
			if (indicesString.IsNullOrEmpty()) throw new Exception("can not parse an empty string to Indices");
			var indices = indicesString.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
			if (indices.Contains("_all")) return Indices.All;
			return Index(indices.Select(i => (IndexName)i));
		}

		public static implicit operator Indices(string indicesString) => Parse(indicesString);
		public static implicit operator Indices(ManyIndices many) => new Indices(many);
		public static implicit operator Indices(IndexName[] many) => new ManyIndices(many);
		public static implicit operator Indices(IndexName index) => new ManyIndices(new[] { index });
		public static implicit operator Indices(Type type) => new ManyIndices(new IndexName[] { type });

		string IUrlParameter.GetString(IConnectionConfigurationValues settings)
		{
			return this.Match(
				all => "_all",
				many =>
				{
					var nestSettings = settings as IConnectionSettingsValues;
					if (nestSettings == null)
						throw new Exception("Tried to pass field name on querysting but it could not be resolved because no nest settings are available");
					var infer = new ElasticInferrer(nestSettings);
					var indices = many.Indices.Select(i => infer.IndexName(i)).Distinct();
					return string.Join(",", indices);
				}
			);

		}
	}
}
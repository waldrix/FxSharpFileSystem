using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFileSystem.Collections
{
	public class TypeCombinationDictionary<T>
	{
		readonly LinkedList<TypeCombinationEntry> _registrations = new LinkedList<TypeCombinationEntry>();

		IEnumerable<TypeCombinationEntry> GetSupportedRegistrations(Type sourceType, Type destinationType)
		{
			return
				_registrations.Where(
					r =>
						r.SourceType.IsAssignableFrom(sourceType) && r.DestinationType.IsAssignableFrom(destinationType));
		}

		public TypeCombinationEntry GetSupportedRegistration(Type sourceType, Type destinationType) { return GetSupportedRegistrations(sourceType, destinationType).FirstOrDefault(); }

		public bool TryGetSupported(Type sourceType, Type destinationType, out T value)
		{
			var r = GetSupportedRegistration(sourceType, destinationType);
			if (r == null)
			{
				value = default(T);
				return false;
			}

			value = r.Value;
			return true;
		}

		// ReSharper disable once UnusedMember.Global
		public void AddFirst(Type sourceType, Type destinationType, T value) { _registrations.AddFirst(new TypeCombinationEntry(sourceType, destinationType, value)); }

		public void AddLast(Type sourceType, Type destinationType, T value) { _registrations.AddLast(new TypeCombinationEntry(sourceType, destinationType, value)); }

		public class TypeCombinationEntry
		{
			public TypeCombinationEntry(Type sourceType, Type destinationType, T value)
			{
				SourceType = sourceType;
				DestinationType = destinationType;
				Value = value;
			}

			public Type SourceType { get; }
			public Type DestinationType { get; }
			public T Value { get; }
		}
	}
}

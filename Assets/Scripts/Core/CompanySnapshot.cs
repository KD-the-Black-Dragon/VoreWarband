// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Detach mutable save/rollback graphs without a serialization round trip (save responsiveness follow-up).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using OdinSerializer;

internal static class CompanySnapshot
{
    sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
        public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
    }
    static readonly MethodInfo ShallowCopy = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly Dictionary<Type, MemberInfo[]> Members = new Dictionary<Type, MemberInfo[]>();
    internal static CompanyCampaign Take(CompanyCampaign state)
    {
        return (CompanyCampaign)Copy(state, new Dictionary<object, object>(new ReferenceComparer()));
    }
    internal static void Restore(CompanyCampaign target, CompanyCampaign snapshot)
    {
        foreach (var field in typeof(CompanyCampaign).GetFields(BindingFlags.Instance | BindingFlags.Public))
            field.SetValue(target, field.GetValue(snapshot));
    }
    static object Collection(Type type, object source)
    {
        var comparer = type.GetProperty("Comparer");
        var constructor = comparer == null ? null : type.GetConstructor(new[] { comparer.PropertyType });
        return constructor == null ? Activator.CreateInstance(type) : constructor.Invoke(new[] { comparer.GetValue(source, null) });
    }
    static object Copy(object value, Dictionary<object, object> copies)
    {
        if (value == null) return null;
        Type type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string || value is decimal || value is DateTime || value is Type) return value;
        // These records are immutable after publication on a generated/enrolled unit.
        if (value is UnitSpeciesBalance) return value;
        if (value is Delegate) throw new InvalidOperationException("Mutable callback targets cannot be captured in a company save.");
        object result;
        if (copies.TryGetValue(value, out result)) return result;
        if (value is UnityEngine.Object) throw new InvalidOperationException("A live Unity object cannot be captured in a company save.");
        var array = value as Array;
        if (array != null)
        {
            result = array.Clone(); copies.Add(value, result);
            if (type.GetElementType().IsPrimitive || type.GetElementType().IsEnum) return result;
            var indices = new int[array.Rank];
            CopyArray(array, (Array)result, indices, 0, copies);
            return result;
        }
        var dictionary = value as IDictionary;
        if (dictionary != null)
        {
            var clone = (IDictionary)Collection(type, value); copies.Add(value, clone);
            foreach (DictionaryEntry entry in dictionary) clone.Add(Copy(entry.Key, copies), Copy(entry.Value, copies));
            return clone;
        }
        var list = value as IList;
        if (list != null)
        {
            var clone = (IList)Collection(type, value); copies.Add(value, clone);
            foreach (object item in list) clone.Add(Copy(item, copies));
            return clone;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            result = Collection(type, value); copies.Add(value, result);
            var add = type.GetMethod("Add");
            foreach (object item in (IEnumerable)value) add.Invoke(result, new[] { Copy(item, copies) });
            return result;
        }
        // Copy only Odin's saved members. Runtime services/caches stay as in the original;
        // the serializer never traverses those references, and game objects are not cloned.
        result = ShallowCopy.Invoke(value, null); copies.Add(value, result);
        MemberInfo[] members;
        lock (Members)
        {
            if (!Members.TryGetValue(type, out members))
                Members.Add(type, members = FormatterUtilities.GetSerializableMembers(type, SerializationPolicies.Unity));
        }
        foreach (var member in members)
            FormatterUtilities.SetMemberValue(member, result, Copy(FormatterUtilities.GetMemberValue(member, value), copies));
        return result;
    }
    static void CopyArray(Array source, Array target, int[] indices, int dimension, Dictionary<object, object> copies)
    {
        for (int index = source.GetLowerBound(dimension); index <= source.GetUpperBound(dimension); index++)
        {
            indices[dimension] = index;
            if (dimension + 1 == indices.Length) target.SetValue(Copy(source.GetValue(indices), copies), indices);
            else CopyArray(source, target, indices, dimension + 1, copies);
        }
    }
}

using System.Text;
using Org.BouncyCastle.Math.EC.Multiplier;

namespace EmotePirate; 

// TODO move to Foxite.Common
public static class Utils {
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value, Func<TValue, TValue> updateFactory) => AddOrUpdate(dict, key, _ => value, (k, v) => updateFactory(v));
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value, Func<TKey, TValue, TValue> updateFactory) => AddOrUpdate(dict, key, _ => value, updateFactory);
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory) {
		if (dict.ContainsKey(key)) {
			dict[key] = updateFactory(key, dict[key]);
		} else {
			dict[key] = addFactory(key);
		}
		return dict[key];
	}

	/// <summary>
	/// Utilize the efficiency of StringBuilder and its AppendFormat function while keeping the sugar of interpolated strings.
	/// </summary>
	public static StringBuilder AppendInterpolated(this StringBuilder sb, FormattableString fs) => sb.AppendFormat(fs.Format, fs.GetArguments());
	
	// TODO (in foxite.common) other projections
	public static Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source) => FirstOrDefaultAsync(source, _ => true); 
	public static async Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate) {
		await using IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator();
		while (await enumerator.MoveNextAsync()) {
			if (predicate(enumerator.Current)) {
				return enumerator.Current;
			}
		}
		return default;
	}
}

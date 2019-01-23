﻿using PokeApi.Net.Directives;
using PokeApi.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PokeApi.Net.Data
{
    /// <summary>
    /// Manages caches for all types
    /// </summary>
    public class CacheManager
    {
        private readonly Dictionary<System.Type, GenericCache<ICanBeCached>> _allCaches;
        private readonly List<System.Type> ApiTypes =
            Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.IsDefined(typeof(ApiEndpointAttribute), false))
            .ToList();

        /// <summary>
        /// Constructor
        /// </summary>
        public CacheManager()
        {
            _allCaches = new Dictionary<System.Type, GenericCache<ICanBeCached>>();
            foreach (System.Type type in ApiTypes) 
            {
                _allCaches.Add(type, new GenericCache<ICanBeCached>());
            }
        }

        /// <summary>
        /// Caches a value
        /// </summary>
        /// <typeparam name="T">Type of object to be cached</typeparam>
        /// <param name="obj">Object to cache</param>
        /// <exception cref="NotSupportedException">The given type is not supported for searching
        /// via PokeAPI</exception>
        public void Store<T>(T obj) where T : class, ICanBeCached
        {
            System.Type matchingType = ApiTypes.FirstOrDefault(type => type == obj.GetType());
            if (matchingType == null)
            {
                throw new NotSupportedException();
            }

            _allCaches[matchingType].Store(obj);
        }

        /// <summary>
        /// Gets a value from cache by id or null if not cached or if not found
        /// </summary>
        /// <typeparam name="T">Type of object to get</typeparam>
        /// <param name="id">Id of the resource</param>
        /// <returns>The cached object or null if not found</returns>
        public T Get<T>(int id) where T : class, ICanBeCached
        {
            System.Type type = typeof(T);
            _allCaches[type].Cache.TryGetValue(id, out ICanBeCached value);
            return value as T;
        }

        /// <summary>
        /// Gets a value from cache by name or null if not cached or if not found
        /// </summary>
        /// <typeparam name="T">Type of object to get</typeparam>
        /// <param name="name">Name of the resource</param>
        /// <returns>The cached object or null if not found</returns>
        public T Get<T>(string name) where T : class, ICanBeCached
        {
            System.Type type = typeof(T);
            PropertyInfo nameProperty = type.GetProperties()
                .FirstOrDefault(property => property.Name.Equals("Name"));
            if (nameProperty == null)
            {
                return null;
            }

            ICanBeCached matchingObject = null;
            foreach (ICanBeCached cacheObj in _allCaches[type].Cache.Values)
            {
                // we wouldn't be here without knowing that T has a Name property
                string value = nameProperty.GetValue(cacheObj) as string;
                if (value.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    matchingObject = cacheObj;
                    break;
                }
            }

            return matchingObject as T;
        }

        /// <summary>
        /// Clears all caches
        /// </summary>
        public void ClearAll()
        {
            foreach (GenericCache<ICanBeCached> cache in _allCaches.Values)
            {
                cache.Clear();
            }
        }

        /// <summary>
        /// Clears a specific cache
        /// </summary>
        /// <typeparam name="T">The type of cache to clear</typeparam>
        public void Clear<T>() where T : class, ICanBeCached
        {
            System.Type type = typeof(T);
            _allCaches[type].Clear();
        }

        private class GenericCache<T> where T : class, ICanBeCached
        {
            /// <summary>
            /// The underlying data store for the cache
            /// </summary>
            public readonly Dictionary<int, T> Cache;

            /// <summary>
            /// Constructor
            /// </summary>
            public GenericCache()
            {
                Cache = new Dictionary<int, T>();
            }

            /// <summary>
            /// Stores an object in cache
            /// </summary>
            /// <param name="obj">The object to store</param>
            public void Store(T obj)
            {
                Cache.Add(obj.Id, obj);
            }

            /// <summary>
            /// Clears all cache data
            /// </summary>
            public void Clear()
            {
                Cache.Clear();
            }
        }
    }
}
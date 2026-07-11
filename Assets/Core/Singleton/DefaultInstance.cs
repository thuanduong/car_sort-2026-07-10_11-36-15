using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Singleton
{
	public abstract class IInitializer
	{
		protected abstract bool HasInit { get; set; }
		protected abstract void Init();
	}

	/// <summary>
	/// Support method for create default instance class
	/// </summary>
	public abstract class DefaultInstance<T> : IInitializer where T : DefaultInstance<T>, new()
	{
		private static T _instance = new T();
		
		public static T Default
		{
			get
			{
				if (_instance == null)
				{
					_instance = new T();
				}
				if (!_instance.HasInit)
				{
					_instance.Init();
					_instance.HasInit = true;
				}
				return _instance;
			}
		}

		private bool _hasInit = false;

		public virtual void DestroyInstance()
		{
			_instance = null;
		}

		#region implemented abstract members of IInitializer

		protected override bool HasInit
		{
			get
			{
				return _hasInit;
			}
			set
			{
				_hasInit = value;
			}
		}

		#endregion
	}

}

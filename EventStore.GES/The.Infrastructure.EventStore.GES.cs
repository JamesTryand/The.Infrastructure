using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Infrastructure {
	
	public class GetEventStoreRepository<T> : IRepository<T> where T : AggregateRoot, new()
	{
		public void Save(AggregateRoot aggregate, int expectedVersion) {
			
		}
		
        public T GetById(Guid id) {
			// if(version <= 0)
			// 	throw new InvalidOper
			
			return GetById(id, int.MaxValue);
		}
		
		public T GetById(Guid id, int version) {
			
		}
	} 
}
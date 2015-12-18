using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;

namespace DG.XrmContext {

    public enum EmptyEnum { }

    public abstract partial class ExtendedEntity<State, Status> : Entity
        where State : struct, IComparable, IConvertible, IFormattable
        where Status : struct, IComparable, IConvertible, IFormattable {

        public ExtendedEntity(string entityName) : base(entityName) { }

        public ExtendedEntity(string entityName, Guid id) : base(entityName) {
            Id = id;
        }

        protected string GetDebuggerDisplay(string primaryNameAttribute) {
            string display = GetType().Name;

            var name = GetAttributeValue<string>(primaryNameAttribute);
            if (!string.IsNullOrEmpty(name)) display += string.Format(" ({0})", name);
            if (Id != Guid.Empty) display += string.Format(" [{0}]", Id);

            return display;
        }

        protected T? GetOptionSetValue<T>(string attributeName) where T : struct, IComparable, IConvertible, IFormattable {
            var optionSet = GetAttributeValue<OptionSetValue>(attributeName);
            if (optionSet != null) {
                return (T)Enum.ToObject(typeof(T), optionSet.Value);
            } else {
                return null;
            }
        }

        protected void SetOptionSetValue<T>(string attributeName, T value) {
            if (value != null) {
                SetAttributeValue(attributeName, new OptionSetValue((int)(object)value));
            } else {
                SetAttributeValue(attributeName, null);
            }
        }

        protected decimal? GetMoneyValue(string attributeName) {
            var money = GetAttributeValue<Money>(attributeName);
            if (money != null) {
                return money.Value;
            } else {
                return null;
            }
        }

        protected void SetMoneyValue(string attributeName, decimal? value) {
            if (value.HasValue) {
                SetAttributeValue(attributeName, new Money(value.Value));
            } else {
                SetAttributeValue(attributeName, null);
            }
        }

        protected IEnumerable<T> GetEntityCollection<T>(string attributeName) where T : Entity {
            var collection = GetAttributeValue<EntityCollection>(attributeName);
            if (collection != null && collection.Entities != null) {
                return collection.Entities.Select(x => x as T);
            } else {
                return null;
            }
        }

        protected void SetEntityCollection<T>(string attributeName, IEnumerable<T> entities) where T : Entity {
            if (entities != null) {
                SetAttributeValue(attributeName, new EntityCollection(new List<Entity>(entities)));
            } else {
                SetAttributeValue(attributeName, null);
            }
        }

        protected void SetId(string primaryIdAttribute, Guid? guid) {
            base.Id = guid.GetValueOrDefault();
            SetAttributeValue(primaryIdAttribute, guid);
        }

        public SetStateResponse SetState(IOrganizationService service, State state) {
            return SetState(service, state, (Status)(object)-1);
        }

        public SetStateResponse SetState(IOrganizationService service, State state, Status status) {
            var resp = (SetStateResponse)service.Execute(GetSetStateRequest(state, status));
            return resp;
        }

        public SetStateRequest GetSetStateRequest(State state, Status status) {
            var req = new SetStateRequest();
            req.EntityMoniker = ToEntityReference();
            req.State = new OptionSetValue((int)(object)state);
            req.Status = new OptionSetValue((int)(object)status);
            return req;
        }
    }

    public abstract partial class ExtendedOrganizationServiceContext : OrganizationServiceContext {

        private IOrganizationService service;

        public ExtendedOrganizationServiceContext(IOrganizationService service) :
            base(service) {
            this.service = service;
        }

        public U Load<T, U>(T entity, Expression<Func<T, U>> loaderFunc) where T : Entity {
            LoadProperty(entity, GetMemberName(loaderFunc));
            return loaderFunc.Compile().Invoke(entity);
        }

        public IEnumerable<U> LoadEnumeration<T, U>(T entity, Expression<Func<T, IEnumerable<U>>> loaderFunc) where T : Entity {
            return Load(entity, loaderFunc) ?? new List<U>();
        }

        public SetStateResponse SetState<T, U>(ExtendedEntity<T, U> entity, T state)
                where T : struct, IComparable, IConvertible, IFormattable
                where U : struct, IComparable, IConvertible, IFormattable {
            return entity.SetState(service, state);
        }

        public SetStateResponse SetState<T, U>(ExtendedEntity<T, U> entity, T state, U status)
                where T : struct, IComparable, IConvertible, IFormattable
                where U : struct, IComparable, IConvertible, IFormattable {
            return entity.SetState(service, state, status);
        }

        private static string GetMemberName<T, U>(Expression<Func<T, U>> lambda) {
            MemberExpression body = lambda.Body as MemberExpression;
            if (body == null) {
                UnaryExpression ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }
            return body.Member.Name;
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ProcessEx.Security
{
    public abstract class SecurityAttributes
    {
        public bool InheritHandle { get; set; }
        public NativeObjectSecurity? SecurityDescriptor { get; set; }
    }

    public abstract class NativeSecurity : NativeObjectSecurity
    {
        protected NativeSecurity(bool isContainer, ResourceType resourceType)
            : base(isContainer, resourceType) { }

        protected NativeSecurity(bool isContainer, ResourceType resourceType, SafeHandle? handle,
            AccessControlSections includeSections) : base(isContainer, resourceType, handle, includeSections) { }

        public new void AddAccessRule(AccessRule rule) { base.AddAccessRule(rule); }
        public new void AddAuditRule(AuditRule rule) { base.AddAuditRule(rule); }

        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask,
            bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            throw new NotImplementedException();
        }

        public AuthorizationRuleCollection GetAccessRules(Type targetType)
        {
            return GetAccessRules(true, false, targetType);
        }

        public new void Persist(SafeHandle handle, AccessControlSections includeSections)
        {
            base.Persist(handle, includeSections);
        }
    }

    public abstract class NativeSecurity<TRight, TRule> : NativeSecurity
        where TRule : AccessRule
    {
        public override Type AccessRightType { get { return typeof(TRight); } }
        public override Type AccessRuleType { get { return typeof(TRule); } }
        public override Type AuditRuleType { get { throw new NotImplementedException(); } }

        protected NativeSecurity(ResourceType resourceType) : base(false, resourceType) { }

        protected NativeSecurity(ResourceType resourceType, SafeHandle handle,
            AccessControlSections includeSections)
            : base(false, resourceType, handle, includeSections) { }
    }
}

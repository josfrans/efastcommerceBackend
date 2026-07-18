using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IStoreInvitationRepository _invitationRepository;
        private readonly IStoreSubscriptionRepository _subscriptionRepository;

        public InvitationService(
            IStoreInvitationRepository invitationRepository,
            IStoreSubscriptionRepository subscriptionRepository)
        {
            _invitationRepository = invitationRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<StoreInvitation> GenerateInvitationAsync(Guid tenantId, Guid? referrerUserId, int expiresHours)
        {
            var randomBytes = new byte[16];
            RandomNumberGenerator.Fill(randomBytes);
            // Convert to a clean URL-friendly token (alphanumeric, no special chars)
            string token = Convert.ToHexString(randomBytes).ToLower().Substring(0, 16);

            var invitation = new StoreInvitation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ReferrerUserId = referrerUserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(expiresHours),
                CreatedAt = DateTime.UtcNow
            };

            await _invitationRepository.AddAsync(invitation);
            return invitation;
        }

        public async Task<IEnumerable<StoreInvitation>> GetInvitationsByTenantAsync(Guid tenantId)
        {
            return await _invitationRepository.GetInvitationsByTenantAsync(tenantId);
        }

        public async Task<bool> DeleteInvitationAsync(Guid id, Guid tenantId)
        {
            var invitation = await _invitationRepository.GetByIdAsync(id);
            if (invitation == null || invitation.TenantId != tenantId)
            {
                return false;
            }

            await _invitationRepository.DeleteAsync(invitation);
            return true;
        }

        public async Task<StoreInvitation?> ValidateTokenAsync(string token)
        {
            var invitation = await _invitationRepository.GetByTokenAsync(token);
            if (invitation == null)
            {
                return null;
            }

            if (DateTime.UtcNow > invitation.ExpiresAt)
            {
                return null; // Link has expired
            }

            return invitation;
        }

        public async Task<IEnumerable<StoreSubscription>> GetCustomersByTenantAsync(Guid tenantId)
        {
            return await _subscriptionRepository.GetSubscriptionsByTenantAsync(tenantId);
        }

        public async Task<bool> RemoveCustomerAsync(Guid userId, Guid tenantId)
        {
            var subscription = await _subscriptionRepository.GetSubscriptionAsync(tenantId, userId);
            if (subscription == null)
            {
                return false;
            }

            subscription.Status = "Revoked";
            await _subscriptionRepository.UpdateAsync(subscription);
            return true;
        }

        public async Task<StoreSubscription> SubscribeUserAsync(Guid userId, string token)
        {
            var invitation = await ValidateTokenAsync(token);
            if (invitation == null)
            {
                throw new InvalidOperationException("El enlace de invitación no es válido o ha expirado.");
            }

            // Check if already subscribed to this tenant
            var existingSub = await _subscriptionRepository.GetSubscriptionAsync(invitation.TenantId, userId);
            if (existingSub != null)
            {
                if (existingSub.Status == "Revoked")
                {
                    existingSub.Status = "Active";
                    existingSub.SubscribedAt = DateTime.UtcNow;
                    // Solo actualizamos ReferredByUserId si estaba nulo, para respetar el referente original
                    if (existingSub.ReferredByUserId == null && invitation.ReferrerUserId != null)
                    {
                        existingSub.ReferredByUserId = invitation.ReferrerUserId;
                    }
                    await _subscriptionRepository.UpdateAsync(existingSub);
                }
                return existingSub; // Already subscribed, return existing
            }

            var subscription = new StoreSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                UserId = userId,
                ReferredByUserId = invitation.ReferrerUserId,
                SubscribedAt = DateTime.UtcNow,
                Status = "Active"
            };

            await _subscriptionRepository.AddAsync(subscription);
            return subscription;
        }

        public async Task<bool> IsUserSubscribedAsync(Guid userId, Guid tenantId)
        {
            var subscription = await _subscriptionRepository.GetSubscriptionAsync(tenantId, userId);
            return subscription != null && subscription.Status == "Active";
        }
    }
}

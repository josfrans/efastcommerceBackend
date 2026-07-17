using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class ReferralService : IReferralService
    {
        private readonly IStoreReferralLinkRepository _referralRepository;
        private readonly IStoreSubscriptionRepository _subscriptionRepository;

        public ReferralService(
            IStoreReferralLinkRepository referralRepository,
            IStoreSubscriptionRepository subscriptionRepository)
        {
            _referralRepository = referralRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<StoreReferralLink> GenerateReferralAsync(Guid tenantId, Guid referrerId, int expiresHours)
        {
            var randomBytes = new byte[16];
            RandomNumberGenerator.Fill(randomBytes);
            // Convert to a clean URL-friendly token (alphanumeric, no special chars)
            string token = "REF-" + Convert.ToHexString(randomBytes).ToLower().Substring(0, 12);

            var referral = new StoreReferralLink
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ReferrerUserId = referrerId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(expiresHours),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            await _referralRepository.AddAsync(referral);
            return referral;
        }

        public async Task<StoreReferralLink?> ValidateTokenAsync(string token)
        {
            var referral = await _referralRepository.GetByTokenAsync(token);
            if (referral == null || referral.IsUsed)
            {
                return null;
            }

            if (DateTime.UtcNow > referral.ExpiresAt)
            {
                return null; // Link has expired
            }

            return referral;
        }

        public async Task<StoreSubscription> SubscribeReferredUserAsync(Guid userId, string token)
        {
            var referral = await ValidateTokenAsync(token);
            if (referral == null)
            {
                throw new InvalidOperationException("El enlace de referido no es válido, ha sido usado o ha expirado.");
            }

            // Marcar el código de referido como usado
            referral.IsUsed = true;
            await _referralRepository.UpdateAsync(referral);

            // Check if already subscribed to this tenant
            var existingSub = await _subscriptionRepository.GetSubscriptionAsync(referral.TenantId, userId);
            if (existingSub != null)
            {
                if (existingSub.Status == "Revoked")
                {
                    existingSub.Status = "Active";
                    existingSub.SubscribedAt = DateTime.UtcNow;
                    // Solo actualizamos ReferredByUserId si estaba nulo, para respetar el referente original si ya lo tenía
                    if (existingSub.ReferredByUserId == null)
                    {
                        existingSub.ReferredByUserId = referral.ReferrerUserId;
                    }
                    await _subscriptionRepository.UpdateAsync(existingSub);
                }
                return existingSub; 
            }

            var subscription = new StoreSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = referral.TenantId,
                UserId = userId,
                ReferredByUserId = referral.ReferrerUserId,
                SubscribedAt = DateTime.UtcNow,
                Status = "Active"
            };

            await _subscriptionRepository.AddAsync(subscription);
            return subscription;
        }

        public async Task<IEnumerable<StoreReferralLink>> GetReferralsByTenantAsync(Guid tenantId)
        {
            return await _referralRepository.GetByTenantAsync(tenantId);
        }
    }
}

using BloodBankManager.Models;

namespace BloodBankManager.Services
{
    /// <summary>
    /// Service để kiểm tra tương thích nhóm máu
    /// Quy tắc: 
    /// - O- có thể cho tất cả (Universal Donor)
    /// - AB+ có thể nhận từ tất cả (Universal Recipient)
    /// - A có thể nhận từ A, O
    /// - B có thể nhận từ B, O
    /// - AB có thể nhận từ AB, A, B, O
    /// </summary>
    public interface IBloodCompatibilityService
    {
        bool IsCompatible(BloodTypeEnum donorBloodType, BloodTypeEnum recipientBloodType);
        List<BloodTypeEnum> GetCompatibleDonorTypes(BloodTypeEnum recipientBloodType);
        List<BloodTypeEnum> GetCompatibleRecipientTypes(BloodTypeEnum donorBloodType);
        string GetBloodTypeDescription(BloodTypeEnum bloodType);
    }

    public class BloodCompatibilityService : IBloodCompatibilityService
    {
        /// <summary>
        /// Kiểm tra xem máu của người hiến có tương thích với người nhận không
        /// </summary>
        public bool IsCompatible(BloodTypeEnum donorBloodType, BloodTypeEnum recipientBloodType)
        {
            return GetCompatibleDonorTypes(recipientBloodType).Contains(donorBloodType);
        }

        /// <summary>
        /// Lấy danh sách nhóm máu có thể hiến cho người nhận
        /// </summary>
        public List<BloodTypeEnum> GetCompatibleDonorTypes(BloodTypeEnum recipientBloodType)
        {
            return recipientBloodType switch
            {
                // O- can only receive O-
                BloodTypeEnum.O_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE
                },

                // O+ can receive O-, O+
                BloodTypeEnum.O_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.O_POSITIVE
                },

                // A- can receive O-, A-
                BloodTypeEnum.A_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.A_NEGATIVE
                },

                // A+ can receive O-, O+, A-, A+
                BloodTypeEnum.A_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.O_POSITIVE,
                    BloodTypeEnum.A_NEGATIVE,
                    BloodTypeEnum.A_POSITIVE
                },

                // B- can receive O-, B-
                BloodTypeEnum.B_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.B_NEGATIVE
                },

                // B+ can receive O-, O+, B-, B+
                BloodTypeEnum.B_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.O_POSITIVE,
                    BloodTypeEnum.B_NEGATIVE,
                    BloodTypeEnum.B_POSITIVE
                },

                // AB- can receive O-, A-, B-, AB-
                BloodTypeEnum.AB_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.A_NEGATIVE,
                    BloodTypeEnum.B_NEGATIVE,
                    BloodTypeEnum.AB_NEGATIVE
                },

                // AB+ can receive all (Universal Recipient)
                BloodTypeEnum.AB_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.O_POSITIVE,
                    BloodTypeEnum.A_NEGATIVE,
                    BloodTypeEnum.A_POSITIVE,
                    BloodTypeEnum.B_NEGATIVE,
                    BloodTypeEnum.B_POSITIVE,
                    BloodTypeEnum.AB_NEGATIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                _ => new List<BloodTypeEnum>()
            };
        }

        /// <summary>
        /// Lấy danh sách nhóm máu có thể nhận máu từ người hiến
        /// </summary>
        public List<BloodTypeEnum> GetCompatibleRecipientTypes(BloodTypeEnum donorBloodType)
        {
            return donorBloodType switch
            {
                // O- is universal donor
                BloodTypeEnum.O_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_NEGATIVE,
                    BloodTypeEnum.O_POSITIVE,
                    BloodTypeEnum.A_NEGATIVE,
                    BloodTypeEnum.A_POSITIVE,
                    BloodTypeEnum.B_NEGATIVE,
                    BloodTypeEnum.B_POSITIVE,
                    BloodTypeEnum.AB_NEGATIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // O+ can donate to O+, A+, B+, AB+
                BloodTypeEnum.O_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.O_POSITIVE,
                    BloodTypeEnum.A_POSITIVE,
                    BloodTypeEnum.B_POSITIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // A- can donate to A-, A+, AB-, AB+
                BloodTypeEnum.A_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.A_NEGATIVE,
                    BloodTypeEnum.A_POSITIVE,
                    BloodTypeEnum.AB_NEGATIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // A+ can donate to A+, AB+
                BloodTypeEnum.A_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.A_POSITIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // B- can donate to B-, B+, AB-, AB+
                BloodTypeEnum.B_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.B_NEGATIVE,
                    BloodTypeEnum.B_POSITIVE,
                    BloodTypeEnum.AB_NEGATIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // B+ can donate to B+, AB+
                BloodTypeEnum.B_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.B_POSITIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // AB- can donate to AB-, AB+
                BloodTypeEnum.AB_NEGATIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.AB_NEGATIVE,
                    BloodTypeEnum.AB_POSITIVE
                },

                // AB+ can only donate to AB+
                BloodTypeEnum.AB_POSITIVE => new List<BloodTypeEnum>
                {
                    BloodTypeEnum.AB_POSITIVE
                },

                _ => new List<BloodTypeEnum>()
            };
        }

        public string GetBloodTypeDescription(BloodTypeEnum bloodType)
        {
            return bloodType switch
            {
                BloodTypeEnum.O_NEGATIVE => "O âm tính (O-) - Nhóm máu phổ biến, có thể hiến cho mọi nhóm",
                BloodTypeEnum.O_POSITIVE => "O dương tính (O+) - Nhóm máu phổ biến",
                BloodTypeEnum.A_NEGATIVE => "A âm tính (A-)",
                BloodTypeEnum.A_POSITIVE => "A dương tính (A+) - Nhóm máu phổ biến",
                BloodTypeEnum.B_NEGATIVE => "B âm tính (B-)",
                BloodTypeEnum.B_POSITIVE => "B dương tính (B+)",
                BloodTypeEnum.AB_NEGATIVE => "AB âm tính (AB-) - Nhóm máu hiếm",
                BloodTypeEnum.AB_POSITIVE => "AB dương tính (AB+) - Có thể nhận từ mọi nhóm (Universal Recipient)",
                _ => "Không xác định"
            };
        }
    }
}

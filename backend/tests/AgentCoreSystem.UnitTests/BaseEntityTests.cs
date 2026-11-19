// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Xunit;

namespace AgentCoreSystem.UnitTests
{
    public class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class BaseEntityTests
    {
        [Fact]
        public void BaseEntity_Should_Generate_Valid_UUIDv7_ByDefault()
        {
            // Act
            var entity1 = new TestEntity { Name = "Entity 1" };
            var entity2 = new TestEntity { Name = "Entity 2" };

            // Assert
            Assert.NotEqual(Guid.Empty, entity1.Id);
            Assert.NotEqual(Guid.Empty, entity2.Id);
            Assert.NotEqual(entity1.Id, entity2.Id);

            // Version 7 check: version number is in the high 4 bits of the 7th byte
            var bytes1 = entity1.Id.ToByteArray();
            // In .NET GUID layout, byte 7 (little-endian byte 7) contains the version
            Assert.Equal(7, entity1.Id.Version);
        }

        [Fact]
        public void BaseEntity_Should_Have_Default_Audit_Properties()
        {
            // Act
            var entity = new TestEntity { Name = "Test" };

            // Assert
            Assert.False(entity.IsDeleted);
            Assert.Null(entity.DeletedDate);
            Assert.Null(entity.UpdatedDate);
            Assert.True((DateTimeOffset.UtcNow - entity.CreatedDate).TotalSeconds < 5);
        }

        [Fact]
        public void Result_Success_Should_Contain_Value()
        {
            // Act
            var result = Result.Success("Sample Data");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal("Sample Data", result.Value);
            Assert.Equal(Error.None, result.Error);
        }

        [Fact]
        public void Result_Failure_Should_Contain_Error()
        {
            // Arrange
            var error = Error.NotFound("User.NotFound", "Không tìm thấy người dùng.");

            // Act
            var result = Result.Failure<string>(error);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
        }
    }
}


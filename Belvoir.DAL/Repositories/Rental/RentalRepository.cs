﻿using Belvoir.DAL.Models;
using Belvoir.DAL.Models.Query;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Belvoir.DAL.Repositories.Rental
{
    public interface IRentalRepository
    {
        public Task<int> CetegoryExist(Guid id);

        public Task<int> AddRentalProductAsync(RentalProduct rentalProduct,Guid Userid);

        public Task<IEnumerable<(RentalProduct, RentalImage)>> GetRentalProductsAsync(RentalQuery query);

        public  Task<RentalProduct> GetRentalProductById(Guid rentalId);

        public Task<IEnumerable<RentalImage>> GetRentalImagesByProductId(Guid id);


        public Task<int> RentalProductAsDeleted(Guid rentalId, Guid userId);

        public Task<int> UpdateRentalProduct(RentalProduct rentalProduct);

        public Task<int> DeleteRentalImages(Guid rentalId);

        public Task<int> AddRentalImage(Guid rentalId, string imagePath, bool isPrimary);
        public  Task<int> AddWhishlist(Guid userid, Guid productid);
        public  Task<IEnumerable<(RentalWhishlist, RentalImage)>> GetWishlist(Guid userId);
        public Task<int> ExistItem(Guid userId, Guid productId);




    }
    public class RentalRepository:IRentalRepository
    {
        public readonly IDbConnection _connection;
        

        public RentalRepository(IDbConnection connection) { 
        _connection = connection;
        }
        public async Task<int> CetegoryExist(Guid id)
        {
            var fabric = await _connection.ExecuteScalarAsync<int>("select count(*) from FabricCategory where id=@id ", new { id = id });
            return fabric;

        }

        public async Task<int> AddRentalProductAsync(RentalProduct rentalProduct,Guid userid)
        {
            var query = @"
            INSERT INTO RentalProduct (Id, Title, Description, OfferPrice, Price, FabricType, Gender, GarmentType, isDeleted, CreatedAt, CreatedBy)
            VALUES (@Id, @Title, @Description, @OfferPrice, @Price, @FabricType, @Gender, @GarmentType, @IsDeleted, @CreatedAt, @CreatedBy)";

            Console.WriteLine(rentalProduct);
            var response =await _connection.ExecuteAsync(query, rentalProduct);

            return response ;
        }




        public async Task<IEnumerable<(RentalProduct, RentalImage)>> GetRentalProductsAsync(RentalQuery query)
        {
            var offset = (query.pageNo - 1) * query.pageSize;

            var spName = "GetRentalProducts";
            var parameters = new DynamicParameters();
            parameters.Add("@p_name", query.searchName);
            parameters.Add("@p_minPrice", query.minPrice);
            parameters.Add("@p_maxPrice", query.maxPrice);
            parameters.Add("@p_gender", query.gender);
            parameters.Add("@p_garmentType", query.garmentType);
            parameters.Add("@p_fabricType", query.fabric_type);
            parameters.Add("@p_pageSize", query.pageSize);
            parameters.Add("@p_offset", offset);

            return await _connection.QueryAsync<RentalProduct, RentalImage, (RentalProduct, RentalImage)>(
                spName,
                (rentalProduct, rentalImage) => (rentalProduct, rentalImage),
                parameters,
                commandType:CommandType.StoredProcedure
            );
        }


        public async Task<RentalProduct> GetRentalProductById(Guid rentalId)
        {
            return await _connection.QueryFirstOrDefaultAsync<RentalProduct>(
                "SELECT * FROM RentalProduct WHERE Id = @Id and isdeleted=false",new { Id = rentalId });
        }

        public async Task<IEnumerable<RentalImage>> GetRentalImagesByProductId(Guid id)
        {
            var query = "SELECT * FROM RentalImage WHERE productid = @Id";
            return await _connection.QueryAsync<RentalImage>(query, new { Id = id });
        }

        public async Task<int> RentalProductAsDeleted(Guid rentalId, Guid userId)
        {
            return await _connection.ExecuteAsync(
                "UPDATE RentalProduct SET isdeleted = @status, updatedat = @time, updatedby = @user WHERE Id = @Id",
                new { Id = rentalId, status = true, time = DateTime.UtcNow, user = userId }
            );
        }


        public async Task<int> UpdateRentalProduct(RentalProduct rentalProduct)
        {
            var query = @"
            UPDATE RentalProduct 
            SET Title = @Title, Description = @Description, OfferPrice = @OfferPrice, 
                Price = @Price, FabricType = @FabricType, Gender = @Gender, 
                GarmentType = @GarmentType, UpdatedBy = @UpdatedBy, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

            return await _connection.ExecuteAsync(query, rentalProduct);
        }

        public async Task<int> DeleteRentalImages(Guid rentalId)
        {
            return await _connection.ExecuteAsync(
                "DELETE FROM RentalImage WHERE productid = @ProductId",
                new { ProductId = rentalId }
            );
        }

        public async Task<int> AddRentalImage(Guid rentalId, string imagePath, bool isPrimary)
        {
            return await _connection.ExecuteAsync(
                "INSERT INTO RentalImage (Id, Imagepath, productid, isprimary) VALUES (@Id, @Imagepath, @ProductId, @IsPrimary)",
                new
                {
                    Id = Guid.NewGuid(),
                    Imagepath = imagePath,
                    ProductId = rentalId,
                    IsPrimary = isPrimary
                }
            );
        }


        

        

        public async Task<int> AddWhishlist(Guid userid, Guid productid)
        {
            return await _connection.ExecuteAsync("insert into Wishlist (user_id,rental_id) values(@usrid,@prid)", new { usrid = userid, prid = productid });
        }


        public async Task<IEnumerable<(RentalWhishlist, RentalImage)>> GetWishlist(Guid userId)
        {
            var query = @"
                SELECT  
                    Wishlist.id as WhishlistId,
                    RentalProduct.Id as ProductId , 
                    RentalProduct.Title, 
                    RentalProduct.Description, 
                    RentalProduct.Price, 
                    RentalProduct.OfferPrice, 
                    RentalImage.Id ,
                    RentalImage.Imagepath
                FROM Wishlist
                JOIN RentalProduct ON Wishlist.rental_id = RentalProduct.id
                LEFT JOIN RentalImage ON RentalProduct.id = RentalImage.productid
                WHERE Wishlist.user_id = @usrid;
            ";

            return await _connection.QueryAsync<RentalWhishlist, RentalImage, (RentalWhishlist, RentalImage)>(
                query,
                (rentalProduct, rentalImage) => (rentalProduct, rentalImage),
                new { usrid = userId },
                splitOn: "id" 
            );
        }




        public async Task<int> ExistItem(Guid userId, Guid productId)
        {
            var query = @"SELECT COUNT(*) 
                  FROM Wishlist 
                  WHERE user_id = @usrid AND rental_id = @prid";

            return await _connection.ExecuteAsync(query, new { usrid = userId, prid = productId });
        }

    }
}

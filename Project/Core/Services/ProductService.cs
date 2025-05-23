﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;
using Infrastructure.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Core.Services
{
    public class ProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        //private readonly Action<string> _logAction;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            //_logAction = message => Console.WriteLine(message);
        }
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _unitOfWork._product.GetAll();
        }
        public async Task<List<Product>> GetAllProductsAsync(Expression<Func<Product, bool>> criteria = null, // where
            Expression<Func<Product, object>>[] includes = null)
        {
            return await _unitOfWork._product.GetAll(criteria, includes);
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _unitOfWork._product.GetById(id);
        }
        //public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        //    => await _unitOfWork._product.Search(p => p.CategoryId == categoryId);

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            // Using the GetAll method with criteria and includes from BaseRepository
            return await _unitOfWork._product.GetAll(
                criteria: p => p.CategoryId == categoryId,
                includes: new Expression<Func<Product, object>>[] { p => p.Category }
    );
        }

        //public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
        //{
        //    var products = await _context.Products
        //        .Where(p => p.CategoryId == categoryId)
        //        .Include(p => p.Category)  // Eager load Category
        //        .ToListAsync();

        //    return products;
        //}

        public async Task<List<Product>> GetFeaturedProductsAsync()
            => await _unitOfWork._product.Search(p => p.IsFeatured);
        // public async Task AddProductAsync(Product post)
        // {
        //     await _unitOfWork._product.Add(post);
        //     await _unitOfWork.CompleteAsync();
        // }
        public async Task AddProductAsync(Product product)
        {
            await _unitOfWork._productRepo.AddAsync(product); // ← use repo, not IBaseRepository
            await _unitOfWork.CompleteAsync();
        }
        public async Task UpdateProductAsync(Product post)
        {
            _unitOfWork._product.Update(post);
            await _unitOfWork.CompleteAsync();
        }
        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork._product.GetById(id);
            if (product != null)
            {
                _unitOfWork._product.Delete(product);
                await _unitOfWork.CompleteAsync();

            }

        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        => await _unitOfWork._product.Search(p =>
            p.Name.Contains(searchTerm) ||
            p.Description.Contains(searchTerm));

        public async Task AddToFavoritesAsync(int customerId, int productId)
        {
            var existing = await _unitOfWork._customerFavoriteProducts
                .Search(cf => cf.CustomerId == customerId && cf.ProductId == productId);

            if (!existing.Any())
            {
                await _unitOfWork._customerFavoriteProducts.Add(new CustomerFavoriteProduct
                {
                    CustomerId = customerId,
                    ProductId = productId
                });
                await _unitOfWork.CompleteAsync();
            }
        }
        public async Task RemoveFromFavoritesAsync(int customerId, int productId)
        {
            var favorites = await _unitOfWork._customerFavoriteProducts
                .Search(cf => cf.CustomerId == customerId && cf.ProductId == productId);

            foreach (var favorite in favorites)
            {
                _unitOfWork._customerFavoriteProducts.Delete(favorite);
            }

            await _unitOfWork.CompleteAsync();
        }

        public async Task<bool> IsProductInFavoritesAsync(int customerId, int productId)
        {
            var favorites = await _unitOfWork._customerFavoriteProducts
                .Search(cf => cf.CustomerId == customerId && cf.ProductId == productId);

            return favorites.Any();
        }

        public async Task<bool> ApplyDiscountAsync(int productId, decimal discountPercentage)
        {
            try
            {
                // Get the product
                var product = await _unitOfWork._product.GetById(productId);
                if (product == null)
                    return false;

                // Validate discount percentage
                if (discountPercentage < 0 || discountPercentage > 100)
                    return false;

                // Apply discount or remove it if 0
                if (discountPercentage > 0)
                {
                    // Convert from percentage (e.g., 25%) to decimal (0.25)
                    product.Discount = discountPercentage / 100m;
                    product.Price = product.Price - (product.Price * product.Discount.Value);
                }
                else
                {
                    product.Discount = null;
                }

                // Update product and save changes
                _unitOfWork._product.Update(product);
                return await _unitOfWork.CompleteAsync() > 0;
            }
            catch (Exception)
            {
                // Log exception here if needed
                return false;
            }
        }

        public async Task<bool> RemoveDiscountAsync(int productId)
        {
            try
            {
                var product = await _unitOfWork._product.GetById(productId);
                if (product == null)
                    return false;

                product.Discount = null;
                _unitOfWork._product.Update(product);
                return await _unitOfWork.CompleteAsync() > 0;
            }
            catch (Exception)
            {
                // Log exception here if needed
                return false;
            }
        }


    }
}
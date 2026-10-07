using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace VoTrongNghia.Services;

/// <summary>
/// Ô chữ để trống thì bind thành chuỗi rỗng, không phải null.
///
/// <para>Mặc định ASP.NET biến ô trống thành <c>null</c> (<c>ConvertEmptyStringToNull</c>).
/// Với biểu mẫu quản trị thì đó là cái bẫy: mọi trường không bắt buộc — thương hiệu,
/// nhóm con, nhãn, mô tả — đều khai kiểu <c>string</c> có giá
/// trị mặc định là rỗng, và mã xử lý gọi thẳng <c>.Trim()</c>. Người dùng xoá trắng
/// một ô rồi bấm Lưu là <c>NullReferenceException</c>, trang lỗi 500, mất hết những
/// gì vừa gõ.</para>
///
/// <para>Vá ở đây một lần cho mọi biểu mẫu, thay vì rắc <c>?? string.Empty</c> khắp
/// nơi rồi sót một chỗ.</para>
/// </summary>
public sealed class ChuoiRongKhongThanhNull : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.ModelType == typeof(string))
        {
            context.DisplayMetadata.ConvertEmptyStringToNull = false;
        }
    }
}

USE LibreriaDb;
GO

UPDATE dbo.Usuario
SET PasswordHash = N'$argon2id$v=19$m=65536,t=3,p=2$hMoB9SdI1DZ2jluCBzKNrw$56NVveYp/elIPZQVGMYG5DZ7BDUa94CUoZuJWeKpmxg',
    FechaModificacion = SYSDATETIME()
WHERE NombreUsuario = N'admin'
  AND PasswordHash = N'AQAAAAIAAYagAAAAEOvA0yb/Qb8i4baG1c5Jyu6QwIsOL+E/+RD5uENxg4ry41ZMRzrjyHVg2gP/OxcS4Q==';
GO

UPDATE dbo.Usuario
SET PasswordHash = N'$argon2id$v=19$m=65536,t=3,p=2$Z4mACIjDrYYGsZ6qUBnowA$8m3mPzVPXCqXntIigdrzBOeeTrtjXdifwvzE6dF84Ig',
    FechaModificacion = SYSDATETIME()
WHERE NombreUsuario = N'vendedor'
  AND PasswordHash = N'AQAAAAIAAYagAAAAEDh3RAQ9Yc7ibdHGSmgl501Txm81qOOimdTVAg2u4ST4xUm/WSwxNhyd8tHeTamYsQ==';
GO

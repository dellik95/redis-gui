export type SectionConfigType<TEntity> = ColumnDefinitionType<TEntity>[];

export type ColumnDefinitionType<TEntity>  = {
  name: string;
  title?: string;
  hidden?: boolean;
  transformer?: (row: TEntity) => string;
};
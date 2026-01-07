---
name: db-migration
description: Memorizer 프로젝트의 PostgreSQL DB 마이그레이션 스킬. 새 테이블 추가, 컬럼 변경, 인덱스 생성 시 사용. SchemaMigrator 기반 넘버링 마이그레이션 패턴 적용.
---

# DB 마이그레이션 스킬

Memorizer 프로젝트에서 PostgreSQL 데이터베이스 스키마를 관리하는 방법을 안내합니다.

## 마이그레이션 구조

```
src/Memorizer/
├── migrations/
│   ├── 001_init.sql
│   ├── 002_xxx.sql
│   ├── ...
│   └── 019_add_bounded_context_column.sql
└── Services/
    └── SchemaMigrator.cs
```

## 마이그레이션 파일 네이밍 규칙

```
{버전번호}_{설명}.sql

예시:
001_init.sql
017_add_prd_share_links.sql
019_add_bounded_context_column.sql
```

- **버전번호**: 3자리 숫자 (001, 002, ...)
- **설명**: snake_case로 변경 내용 요약
- **확장자**: .sql

## 마이그레이션 작성 패턴

### 1. 새 테이블 추가

```sql
-- Migration 0XX: Add {table_name} table
-- 설명

CREATE TABLE IF NOT EXISTS {table_name} (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- 또는
    short_code VARCHAR(6) PRIMARY KEY,

    -- 컬럼 정의
    title TEXT NOT NULL DEFAULT 'Default Title',
    content TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active',

    -- 타임스탬프
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 인덱스 (필요시)
CREATE INDEX IF NOT EXISTS idx_{table_name}_created_at
    ON {table_name}(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_{table_name}_{column}
    ON {table_name}({column});
```

### 2. 컬럼 추가

```sql
-- Migration 0XX: Add {column_name} to {table_name}

ALTER TABLE {table_name}
ADD COLUMN IF NOT EXISTS {column_name} TEXT;

-- 또는 기본값과 함께
ALTER TABLE {table_name}
ADD COLUMN IF NOT EXISTS {column_name} TEXT DEFAULT '';
```

### 3. 벡터 컬럼 추가 (임베딩용)

```sql
-- Migration 0XX: Add embedding to {table_name}

-- 벡터 확장이 필요한 경우
CREATE EXTENSION IF NOT EXISTS vector;

ALTER TABLE {table_name}
ADD COLUMN IF NOT EXISTS embedding VECTOR(384);

-- 벡터 검색 인덱스
CREATE INDEX IF NOT EXISTS idx_{table_name}_embedding
    ON {table_name} USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);
```

### 4. 인덱스만 추가

```sql
-- Migration 0XX: Add index for {purpose}

CREATE INDEX IF NOT EXISTS idx_{table_name}_{column}
    ON {table_name}({column});

-- 복합 인덱스
CREATE INDEX IF NOT EXISTS idx_{table_name}_{col1}_{col2}
    ON {table_name}({col1}, {col2});

-- 조건부 인덱스
CREATE INDEX IF NOT EXISTS idx_{table_name}_active
    ON {table_name}({column}) WHERE status = 'active';
```

### 5. 관계 테이블

```sql
-- Migration 0XX: Add {relationship} table

CREATE TABLE IF NOT EXISTS memory_relationships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    target_memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    relationship_type TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(source_memory_id, target_memory_id, relationship_type)
);

CREATE INDEX IF NOT EXISTS idx_memory_relationships_source
    ON memory_relationships(source_memory_id);

CREATE INDEX IF NOT EXISTS idx_memory_relationships_target
    ON memory_relationships(target_memory_id);
```

## 기존 테이블 스키마 참조

### memories (메인 테이블)

```sql
CREATE TABLE memories (
    id UUID PRIMARY KEY,
    type TEXT NOT NULL,
    content JSONB NOT NULL,
    source TEXT NOT NULL,
    embedding VECTOR(384) NOT NULL,
    tags TEXT[] NOT NULL,
    confidence DOUBLE PRECISION NOT NULL,
    title TEXT,
    text TEXT,
    metadata_embedding VECTOR(384),
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);
```

### askbot_share_links (공유 링크)

```sql
CREATE TABLE askbot_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    session_id VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    referenced_memory_ids TEXT[],
    image_paths TEXT[],
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### prd_share_links (PRD 공유)

```sql
CREATE TABLE prd_share_links (
    short_code VARCHAR(6) PRIMARY KEY,
    title TEXT NOT NULL DEFAULT 'PRD Analysis',
    prd_content TEXT NOT NULL,
    event_storming_result TEXT NOT NULL,
    discussion_result TEXT,
    example_mapping_result TEXT,
    refined_prd TEXT,
    bounded_context TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## SchemaMigrator 동작 방식

1. 앱 시작 시 `SchemaMigrator.MigrateAsync()` 자동 실행
2. `schema_version` 테이블에서 적용된 마이그레이션 확인
3. 미적용 마이그레이션을 버전 순서대로 실행
4. 실행 완료 시 `schema_version`에 기록

```csharp
// Program.cs에서 호출
await SchemaMigrator.MigrateAsync(connectionString, cancellationToken);
```

## 새 마이그레이션 추가 절차

1. **버전 번호 확인**
   ```bash
   ls src/Memorizer/migrations/
   # 가장 높은 번호 + 1
   ```

2. **마이그레이션 파일 생성**
   ```bash
   # 예: 020_add_new_feature.sql
   ```

3. **SQL 작성**
   - `IF NOT EXISTS` / `IF EXISTS` 사용 (멱등성)
   - 트랜잭션 고려

4. **빌드 및 테스트**
   ```bash
   docker-compose -f docker-compose.local-psmon.yml build memorizer
   docker-compose -f docker-compose.local-psmon.yml up -d memorizer
   ```

5. **적용 확인**
   ```sql
   SELECT * FROM schema_version ORDER BY version;
   ```

## 주의사항

1. **멱등성**: `IF NOT EXISTS`, `IF EXISTS` 사용
2. **롤백 없음**: 롤백 스크립트는 별도 관리 필요
3. **순서 의존**: 버전 번호 순서대로 실행됨
4. **운영 DB 주의**: 로컬 테스트 후 적용
5. **백업**: 중요 변경 전 백업 권장

## 자주 사용하는 데이터 타입

| 용도 | PostgreSQL 타입 |
|-----|----------------|
| UUID | `UUID` |
| 짧은 텍스트 | `VARCHAR(n)` |
| 긴 텍스트 | `TEXT` |
| JSON | `JSONB` |
| 배열 | `TEXT[]`, `UUID[]` |
| 벡터 | `VECTOR(384)` |
| 타임스탬프 | `TIMESTAMPTZ` |
| 실수 | `DOUBLE PRECISION` |
| 정수 | `INT`, `BIGINT` |

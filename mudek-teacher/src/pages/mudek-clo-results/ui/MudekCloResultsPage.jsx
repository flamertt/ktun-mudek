import { createColumnHelper } from '@tanstack/react-table'
import { useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'

import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import { DataTable } from '@shared/ui/data-table/DataTable.jsx'
import {
  formatMudekDecimal,
  mudekCloResultTypeTr,
} from '../../course-evaluation/lib/mudekDisplayLabels'
import { useCourseEvaluationMudekData } from '../../course-evaluation/model/useCourseEvaluationMudekData'
import { EvaluationMudekBackToolbar } from '../../course-evaluation/ui/EvaluationMudekBackToolbar.jsx'
import styles from './MudekCloResultsPage.module.css'

const columnHelper = createColumnHelper()

export function MudekCloResultsPage() {
  const { offeringId } = useParams()
  const d = useCourseEvaluationMudekData(offeringId)
  const [filter, setFilter] = useState('')

  const rows = useMemo(() => {
    const list = d.mudekResults?.cloResults ?? d.mudekResults?.CloResults ?? []
    if (!Array.isArray(list)) return []
    return list.map((x) => {
      const rt = x.resultType ?? x.ResultType
      return {
        id: x.id ?? x.Id,
        courseLearningOutcomeId: x.courseLearningOutcomeId ?? x.CourseLearningOutcomeId,
        resultType: rt,
        resultTypeLabel: mudekCloResultTypeTr(rt),
        examId: x.examId ?? x.ExamId,
        achievementScore: x.achievementScore ?? x.AchievementScore,
        combinedAchievementScore: x.combinedAchievementScore ?? x.CombinedAchievementScore,
        surveyScore: x.surveyScore ?? x.SurveyScore,
        surveyDifference: x.surveyDifference ?? x.SurveyDifference,
        updatedAt: x.updatedAt ?? x.UpdatedAt,
      }
    })
  }, [d.mudekResults])

  const sorted = useMemo(() => {
    return [...rows].sort((a, b) => {
      const as = Number(a.combinedAchievementScore ?? a.achievementScore ?? -Infinity)
      const bs = Number(b.combinedAchievementScore ?? b.achievementScore ?? -Infinity)
      return bs - as
    })
  }, [rows])

  const ranked = useMemo(() => {
    if (!sorted.length) return []
    const mid = Math.floor(sorted.length / 2)
    return sorted.map((r, i) => ({
      ...r,
      rankClass: i === 0 ? 'rowBest' : i === sorted.length - 1 ? 'rowWorst' : i === mid ? 'rowAvg' : '',
    }))
  }, [sorted])

  const columns = useMemo(
    () => [
      columnHelper.accessor('courseLearningOutcomeId', {
        header: 'DÖÇ',
        cell: (info) => d.cloById.get(String(info.getValue() ?? '')) ?? d.shortGuid(info.getValue()),
      }),
      columnHelper.accessor('resultTypeLabel', { header: 'Sonuç türü' }),
      columnHelper.accessor('achievementScore', {
        header: 'Başarı skoru',
        cell: (info) => formatMudekDecimal(info.getValue(), 4),
      }),
      columnHelper.accessor('combinedAchievementScore', {
        header: 'Birleşik başarı',
        cell: (info) => formatMudekDecimal(info.getValue(), 4),
      }),
      columnHelper.accessor('surveyScore', {
        header: 'Anket skoru',
        cell: (info) => formatMudekDecimal(info.getValue(), 2),
      }),
      columnHelper.accessor('surveyDifference', {
        header: 'Anket farkı',
        cell: (info) => formatMudekDecimal(info.getValue(), 2),
      }),
      columnHelper.accessor('updatedAt', { header: 'Güncellendi', cell: (info) => d.formatDate(info.getValue()) }),
    ],
    [d],
  )

  return (
    <PageSection title={`${d.title} · MÜDEK · CLO`} description="CLO sonuçları" error={d.error} loading={d.loading}>
      <EvaluationMudekBackToolbar />
      <div className={styles.panel}>
        <h3 className={styles.panelTitle}>MÜDEK · CLO sonuçları</h3>
        <DataTable
          columns={columns}
          data={ranked}
          globalFilter={filter}
          onGlobalFilterChange={setFilter}
          searchPlaceholder="DÖÇ veya sonuç türü ara…"
          isLoading={false}
          disablePagination
          getRowClassName={(row) => row?.rankClass ?? ''}
        />
      </div>
    </PageSection>
  )
}

